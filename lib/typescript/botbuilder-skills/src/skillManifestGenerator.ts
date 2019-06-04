import { LUISAuthoringModels as Models } from '@azure/cognitiveservices-luis-authoring';
import { IBotSettingsBase, ICognitiveModelConfiguration } from 'botbuilder-solutions';
import { ILuisService } from 'botframework-config';
import { readFileSync } from 'fs';
import { get } from 'request-promise-native';
import { IAction, ISkillManifest, IUtteranceSources } from './models';

type LuisModelMap = Map<string, Models.VersionsExportMethodResponse>;

interface ILangEntry<T> {
    language: string;
    item: T;
}

export class SkillManifestGenerator {
    private readonly skillRoute: string = '/api/skill/messages';

    //tslint:disable-next-line: max-func-body-length
    public async generateManifest(
        manifestFile: string,
        appId: string,
        cognitiveModels: Map<string, ICognitiveModelConfiguration>,
        uriBase: string,
        inlineTriggerUtterances: boolean = false
    ): Promise<ISkillManifest> {
        if (!manifestFile) { throw new Error('manifestFile has no value'); }
        if (!appId) { throw new Error('appId has no value'); }
        if (!cognitiveModels) { throw new Error('cognitiveModels has no value'); }
        if (!uriBase) { throw new Error('uriBase has no value'); }

        // Each skill has a manifest template in the root directory and is used as foundation for the generated manifest
        const skillManifest: ISkillManifest = JSON.parse(readFileSync(manifestFile, 'UTF8'));
        if (!skillManifest.id) { throw new Error('Skill manifest ID property was not present in the template manifest file.'); }
        if (!skillManifest.name) { throw new Error('Skill manifest Name property was not present in the template manifest file.'); }

        skillManifest.msAppId = appId;
        skillManifest.endpoint = `${uriBase}${this.skillRoute}`;

        if (skillManifest.iconUrl !== undefined) {
            skillManifest.iconUrl = `${uriBase}/${skillManifest.iconUrl}`;
        }

        // The manifest can either return a pointer to the triggering utterances or include them inline in the manifest
        // If the developer has requested inline, we need to go through all utteranceSource references
        // and retrieve the utterances and insert inline
        if (inlineTriggerUtterances) {
            const localeLuisModelsEntries: ILangEntry<Models.VersionsExportMethodResponse>[] =
            await this.getLocaleLuisModelsEntries(cognitiveModels);

            const localeLuisModels: LuisModelMap = new Map();
            localeLuisModelsEntries.filter((entry: ILangEntry<Models.VersionsExportMethodResponse>) => entry.language)
            .forEach((entry: ILangEntry<Models.VersionsExportMethodResponse>) => {
                localeLuisModels.set(entry.language, entry.item);
            });

            skillManifest.actions.forEach((action: IAction) => {
                // Is this Action triggered by LUIS utterances rather than events?
                if (action.definition.triggers.utteranceSources) {
                    // We will retrieve all utterances from the referenced source
                    // and aggregate into one new aggregated list of utterances per action
                    action.definition.triggers.utterances = [];
                    const utterancesToAdd: string[] = [];

                    // Iterate through each utterance source, one per locale.
                    action.definition.triggers.utteranceSources.forEach((utteranceSource: IUtteranceSources) => {
                        // There may be multiple intents linked to this
                        utteranceSource.source.forEach((source: string) => {
                            // Retrieve the intent mapped to this action trigger
                            const intentIndex: number = source.indexOf('#');
                            if (intentIndex === -1) {
                                throw new Error(`Utterance source for action: ${
                                    action.id
                                } didn't include an intent reference: ${
                                    source
                                }`);
                            }

                            // We now have the name of the LUIS model and the Intent
                            const modelName: string = source.substring(0, intentIndex);
                            const intentToMatch: string = source.substring(intentIndex + 1);

                            // Find the LUIS model from our cache by matching on the locale/modelname
                            const modelKey: string = `${utteranceSource.locale}_${modelName}`.toLowerCase();
                            const model: Models.VersionsExportMethodResponse | undefined = localeLuisModels.get(modelKey);
                            if (model === undefined) {
                                throw new Error(`Utterance source (locale: ${
                                    utteranceSource.locale
                                }) for action: '${
                                    action.id
                                }' references the '${
                                    modelName
                                }' model which cannot be found in the currently deployed configuration.`);
                            }

                            // Validate that the intent in the manifest exists in this LUIS model
                            const intents: Models.HierarchicalModel[] = model.intents || [];

                            const hasMatch: boolean = intents.some((intent: Models.HierarchicalModel) => {
                                const intentName: string = (intent.name || '').toLowerCase();

                                return intentName === intentToMatch.toLowerCase();
                            });

                            if (!hasMatch) {
                                throw new Error(`Utterance source for action: '${
                                    action.id
                                }' references the '${
                                    modelName
                                }' model and '${
                                    intentToMatch
                                }' intent which does not exist.`);
                            }

                            // Retrieve the utterances that match this intent
                            const utterancesList: Models.JSONUtterance[] = model.utterances || [];
                            const utterances: Models.JSONUtterance[] = utterancesList.filter((s: Models.JSONUtterance) => {
                                const sIntent: string = (s.intent || '').toLowerCase();

                                return sIntent === intentToMatch.toLowerCase();
                            });

                            if (!utterances) {
                                throw new Error(`Utterance source for action: '${
                                    action.id
                                }' references the '${
                                    modelName
                                }' model and '${
                                    intentToMatch
                                }' intent which has no utterances.`);
                            }

                            utterances.forEach((utterance: Models.JSONUtterance) => {
                                utterancesToAdd.push(utterance.text || '');
                            });
                        });

                        action.definition.triggers.utterances.push({
                            locale: utteranceSource.locale,
                            text: utterancesToAdd
                        });
                    });
                }
            });
        }

        return skillManifest;
    }

    private async fetchLuisModelContent(luisService: ILuisService): Promise<Models.VersionsExportMethodResponse> {
        const endpoint: string = `https://${
            luisService.region
        }.api.cognitive.microsoft.com/luis/api/v2.0/apps/${
            luisService.appId
        }/versions/${
            luisService.version
        }/export`;

        return get(endpoint, { headers: { 'Ocp-Apim-Subscription-Key': luisService.authoringKey } , json: true});
    }

    private async getLocaleLuisModelsEntries(
        models: Map<string, ICognitiveModelConfiguration>
    ): Promise<ILangEntry<Models.VersionsExportMethodResponse>[]> {
        const entries: [string, ICognitiveModelConfiguration][] = Array.from(models.entries());
        const langLuisEntries: ILangEntry<ILuisService[]>[] = entries.map((entry: [string, ICognitiveModelConfiguration]) => {
            return {
                language: entry[0],
                item: entry[1].languageModels
            };
        });

        const flatLangLuisService: ILangEntry<ILuisService>[] = langLuisEntries
        .reduce(
            (acc: ILangEntry<ILuisService>[], curr: ILangEntry<ILuisService[]>) => {
                const flat: ILangEntry<ILuisService>[] = curr.item.map((luisService: ILuisService) => {
                    return {
                        language: curr.language,
                        item: luisService
                    };
                });

                return acc.concat(flat);
            },
            []);

        return Promise.all(
            flatLangLuisService.map(async (entry: ILangEntry<ILuisService>) => {
                try {
                    const luisModel: Models.VersionsExportMethodResponse = await this.fetchLuisModelContent(entry.item);

                    return {
                        language: `${entry.language}_${entry.item.id}`.toLowerCase(),
                        item: luisModel
                    };
                } catch (error) {
                    return {
                        language: '',
                        //tslint:disable-next-line: no-any
                        item: <any>{}
                    };
                }
            })
        );
    }
}

//tslint:disable-next-line: no-any
export function manifestGenerator(manifestFile: string, botSettings: Partial<IBotSettingsBase>): (req: any, res: any, next: any) => any {
    //tslint:disable-next-line: no-any
    return async (req: any, res: any, next: any): Promise<any> => {
        if (manifestFile === undefined) { throw new Error('manifestFile has no value'); }
        if (botSettings.microsoftAppId === undefined) { throw new Error('botSettings.microsoftAppId has no value'); }
        if (botSettings.cognitiveModels === undefined) { throw new Error('botSettings.cognitiveModels has no value'); }

        const inline: boolean = (req.query.inlineTriggerUtterances || '').toLowerCase() === 'true';
        const scheme: string = req.isSecure() ? 'https' : 'http';
        const host: string = req.headers.host || '';
        const skillUriBase: string = `${scheme}://${host}`;
        const appId: string = botSettings.microsoftAppId;
        const cognitiveModels: Map<string, ICognitiveModelConfiguration> = botSettings.cognitiveModels;

        const generator: SkillManifestGenerator = new SkillManifestGenerator();
        const manifest: ISkillManifest = await generator.generateManifest(manifestFile, appId, cognitiveModels, skillUriBase, inline);
        res.send(200, manifest);

        return next();
    };
}
