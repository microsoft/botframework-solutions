/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { existsSync, readFileSync, writeFileSync } from 'fs';
import { join } from 'path';
import { ConsoleLogger, ILogger } from '../logger';
import { ICognitiveModel, IRefreshConfiguration, ISkillManifest, IAction, ISkillFile, IUtterance  } from '../models';
import { ChildProcessUtils, getDispatchNames, wrapPathWithQuotes, deleteFiles } from '../utils';

export class RefreshSkill {
    public logger: ILogger;
    private readonly childProcessUtils: ChildProcessUtils;
    private readonly configuration: IRefreshConfiguration;
    private tempFiles: string[] = [];

    public constructor(configuration: IRefreshConfiguration, logger?: ILogger) {
        this.configuration = configuration;
        this.logger = logger || new ConsoleLogger();
        this.childProcessUtils = new ChildProcessUtils();
    }

    private async runCommand(command: string[], description: string): Promise<string> {
        this.logger.command(description, command.join(' '));
        const cmd: string = command[0];
        const commandArgs: string[] = command.slice(1)
            .filter((arg: string): string => arg);
        try {
            return await this.childProcessUtils.execute(cmd, commandArgs);
        } catch (err) {
            throw err;
        }
    }

    private async executeDispatchRefresh(dispatchName: string, executionModelByCulture: Map<string, string>): Promise<void> {
        const dispatchRefreshCommand: string[] = ['dispatch', 'refresh'];
        try {
            this.logger.message(`Running dispatch refresh for ${ dispatchName }...`);
            const dispatchJsonFile: string = executionModelByCulture.get('dispatchJsonFile') as string;
            const dispatchJsonFilePath: string = executionModelByCulture.get('dispatchJsonFilePath') as string;
            const dispatchRefreshCommandArguments: string[] = ['--dispatch', '--dataFolder'];
            dispatchRefreshCommandArguments.forEach((argument: string): void => {
                const argumentValue: string = executionModelByCulture.get(argument) as string;
                dispatchRefreshCommand.push(...[argument, argumentValue]);
            });
            await this.runCommand(
                dispatchRefreshCommand,
                `Executing dispatch refresh for the ${ dispatchName } file`);

            if (!existsSync(dispatchJsonFilePath)) {
                throw new Error(`Path to ${ dispatchJsonFile } (${ dispatchJsonFilePath }) leads to a nonexistent file. This may be due to a problem with the 'dispatch refresh' command.`);
            }
        } catch (err) {
            throw new Error(`There was an error in the dispatch refresh command:\nCommand: ${ dispatchRefreshCommand.join(' ') }\n${ err }`);
        }
    }

    private async executeLuisGen(dispatchName: string, executionModelByCulture: Map<string, string>): Promise<void> {
        const luisgenCommand: string[] = ['luisgen'];
        try {
            this.logger.message(`Running LuisGen for ${ dispatchName }...`);
            const dispatchJsonFilePath: string = executionModelByCulture.get('dispatchJsonFilePath') as string;
            const luisgenCommandArguments: string [] = [
                wrapPathWithQuotes(dispatchJsonFilePath),
                `-${ this.configuration.lgLanguage }`,
                '-o'
            ];
            luisgenCommandArguments.forEach((argument: string): void => {
                const argumentValue: string = executionModelByCulture.get(argument) as string;
                luisgenCommand.push(...[argument, argumentValue]);
            });
            await this.runCommand(luisgenCommand, `Executing luisgen for the ${ dispatchName } file`);
        } catch (err) {
            throw new Error(`There was an error in the luisgen command:\nCommand: ${ luisgenCommand.join(' ') }\n${ err }`);
        }
    }

    private getExecutionModelUtterances(luisApp: string, culture: string, intentName: string, dispatchName: string): Map<string, string> {
        const luFile = `${ luisApp }.lu`;
        const luisFile = `${ luisApp }.luis`;
        const luFolderPath: string = join(this.configuration.outFolder, 'deployment', 'resources', 'LU');
        const luFilePath: string = join(luFolderPath, culture, luFile);
        const luisFolderPath: string = join(luFolderPath, culture);
        const luisFilePath: string = join(luisFolderPath, luisFile);
        const dispatchFile = `${ dispatchName }.dispatch`;
        const dispatchFolderPath: string = join(this.configuration.dispatchFolder, culture);
        const dispatchFilePath: string = join(dispatchFolderPath, dispatchFile);
        this.tempFiles.push(luFilePath,luisFilePath);

        // Validate 'ludown' arguments
        if (!existsSync(luFolderPath)) {
            throw new Error(`Path to the LUIS folder (${ luFolderPath }) leads to a nonexistent folder.
Remember to use the argument '--luisFolder' for your Skill's LUIS folder.`);
        } else if (!existsSync(luFilePath)) {
            throw new Error(`Path to the ${ luisApp }.lu file leads to a nonexistent file.
Make sure your Skill's .lu file's name matches your Skill's manifest id`);
        }

        if (!existsSync(this.configuration.dispatchFolder)) {
            throw new Error(`Path to the Dispatch folder (${ this.configuration.dispatchFolder }) leads to a nonexistent folder.
            Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        } else if (!existsSync(dispatchFilePath)) {
            throw new Error(`Path to the ${ dispatchFile } file leads to a nonexistent file.`);
        }

        const executionModelMap: Map<string, string> = new Map();
        executionModelMap.set('luisApp', luisApp);
        executionModelMap.set('luisFile', luisFile);
        executionModelMap.set('luisFilePath', luisFilePath);
        executionModelMap.set('--in', wrapPathWithQuotes(luFilePath));
        executionModelMap.set('--luis_culture', culture);
        executionModelMap.set('--out_folder', wrapPathWithQuotes(luisFolderPath));
        executionModelMap.set('--out', luisFile);
        executionModelMap.set('--type', 'file');
        executionModelMap.set('--name', intentName);
        executionModelMap.set('--filePath', luisFilePath);
        executionModelMap.set('--intentName', intentName);

        return executionModelMap;
    }

    private getExecutionModel(culture: string, dispatchName: string): Map<string, string> {
        const dispatchFile = `${ dispatchName }.dispatch`;
        const dispatchJsonFile = `${ dispatchName }.json`;
        const dispatchFilePath: string = join(this.configuration.dispatchFolder, culture, dispatchFile);
        const dispatchJsonFilePath: string = join(this.configuration.dispatchFolder, culture, dispatchJsonFile);
        const dataFolder: string = join(this.configuration.dispatchFolder, culture);
        if (!existsSync(this.configuration.dispatchFolder)) {
            throw new Error(`Path to the Dispatch folder (${ this.configuration.dispatchFolder }) leads to a nonexistent folder.
Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        } else if (!existsSync(dispatchFilePath)) {
            throw new Error(`Path to the ${ dispatchFile } file leads to a nonexistent file.`);
        }

        const executionModelMap: Map<string, string> = new Map();
        executionModelMap.set('dispatchJsonFile', dispatchJsonFile);
        executionModelMap.set('dispatchJsonFilePath', dispatchJsonFilePath);
        executionModelMap.set('--dispatch', dispatchFilePath);
        executionModelMap.set('--dataFolder', dataFolder);
        executionModelMap.set(wrapPathWithQuotes(dispatchJsonFilePath), '');
        executionModelMap.set(`-${ this.configuration.lgLanguage }`, wrapPathWithQuotes('DispatchLuis'));
        executionModelMap.set('-o', wrapPathWithQuotes(this.configuration.lgOutFolder));

        return executionModelMap;
    }

    private async updateModel(): Promise<void> {
        if (!existsSync(this.configuration.cognitiveModelsFile)) {
            throw new Error(`Could not find the cognitiveModels file (${
                this.configuration.cognitiveModelsFile }). Please provide the '--cognitiveModelsFile' argument.`);
        }
        
        const cognitiveModelsFile: ICognitiveModel = JSON.parse(readFileSync(this.configuration.cognitiveModelsFile, 'UTF8'));
        const dispatchNames: Map<string, string> = getDispatchNames(cognitiveModelsFile);
        const executionsModelMap: Map<string, Map<string, string>> = new Map();
        dispatchNames.forEach((dispatchName: string, culture: string): void => {
            executionsModelMap.set(culture, this.getExecutionModel(culture, dispatchName));
        });

        await Promise.all(Array.from(executionsModelMap.entries())
            .map(async (item: [string, Map<string, string>]): Promise<void> => {
                const culture: string = item[0];
                const executionModelByCulture: Map<string, string> = item[1];
                const dispatchName: string = dispatchNames.get(culture) as string;
                await this.executeDispatchRefresh(dispatchName, executionModelByCulture);
                await this.executeLuisGen(dispatchName, executionModelByCulture);
            }));
    }

    private async processTemporalFiles(luisDictionary: Map<string, string[]>): Promise<void> {
        try {
            const filteredLuisDictionary: [string, string[]][] = Array.from(luisDictionary.entries());

            const cognitiveModelsFile: ICognitiveModel = JSON.parse(readFileSync(this.configuration.cognitiveModelsFile, 'UTF8'));
            const dispatchNames: Map<string, string> = getDispatchNames(cognitiveModelsFile);

            const executionsModelMap: Map<string, [Map<string, string>]> = new Map();
            filteredLuisDictionary.map((item: [string, string[]]): void => {
                const luisCulture: string = item[0];
                const filteredluisApps: string[] = item[1];
                const dispatchName: string = dispatchNames.get(luisCulture) as string;
                filteredluisApps.map((luisApp: string): void => {
                    let intentName: string = luisApp.split('_')[1];
                    if(executionsModelMap.has(luisCulture))
                    {
                        let temporalFileValue: [Map<string, string>] = executionsModelMap.get(luisCulture) || [new Map<string,string>()];
                        temporalFileValue.push(this.getExecutionModelUtterances(luisApp, luisCulture, intentName, dispatchName));
                        executionsModelMap.set(luisCulture, temporalFileValue);
                    }
                    else {
                        executionsModelMap.set(luisCulture, [this.getExecutionModelUtterances(luisApp, luisCulture, intentName, dispatchName)]);
                    }
                });
            }); 

            await Promise.all(Array.from(executionsModelMap.entries())
                .map(async (item: [string, [Map<string, string>]]): Promise<void> => {
                    const culture: string = item[0];
                    const executionmodelbyculture: [Map<string, string>] = item[1];
                    await this.executeLudownParse(culture, executionmodelbyculture);
                }));

        } catch (err) {
            throw new Error(`An error ocurred while creating the temporal files:\n${ err }`);
        }
    }

    private async getTemporalFiles(): Promise<Map<string, string[]>> {
        try {
            let skillsArray: ISkillFile = JSON.parse(readFileSync(join(this.configuration.outFolder, (this.configuration.lgLanguage === 'ts' ? join('src', 'skills.json') : 'skills.json')), 'UTF8'));
            let temporalFiles: Map<string, string[]> = new Map();

            // Each skill manifest in the Assistant is evaluated if it has utterances, and then are grouped by language
            skillsArray.skills.forEach((skill: ISkillManifest): void => {
                let actionId: string[] = [skill.actions[0].id.split('_')[0]];
                let utterancesGroupByLocale: { [key: string]: IUtterance[] } = 
                    skill.actions.filter((action: IAction): IUtterance[] => action.definition.triggers.utterances)
                        ?.reduce((acc: IUtterance[], val: IAction): IUtterance[] => acc.concat(val.definition.triggers.utterances), [])
                        ?.reduce((groupedUtterances: any, utterance: IUtterance): { [key: string]: IUtterance[] } => {
                            groupedUtterances[utterance.locale] = groupedUtterances[utterance.locale] || []; 
                            groupedUtterances[utterance.locale].push(utterance);
                            return groupedUtterances; 
                        }, {});
            
                if (utterancesGroupByLocale) {
                    Object.keys(utterancesGroupByLocale).forEach((locale: string): void => {
                        let textUnifiedByLocale: string[] = [];

                        // All the utterances are unified by language to be processed and appended in the lu file.
                        Object.values(utterancesGroupByLocale[locale]).forEach((utterances): void => {
                            utterances.text.forEach((text): void =>{
                                textUnifiedByLocale.push(text);
                            });
                        });

                        const unifiedUtterances: string = textUnifiedByLocale.map((v: string): string => '- ' + v).join('\n');
                        writeFileSync(`${ join(this.configuration.outFolder, 'deployment', 'resources', 'LU', locale, 'temp_' + actionId + '.lu') }`, '#' + actionId + '\n' + unifiedUtterances );
                        
                        // The names of the just created files are persisted by language.
                        if(temporalFiles.has(locale))
                        {
                            let tempStoringArray: string[] = [];
                            let temporalFileValue: string[] = temporalFiles.get(locale) || [];
                            temporalFileValue.forEach((element): void => {
                                tempStoringArray.push(element);
                            });
                            
                            tempStoringArray.push(`temp_${ actionId }`);
                            temporalFiles.set(locale, tempStoringArray);
                        }
                        else {
                            temporalFiles.set(locale, [`temp_${ actionId }`]);
                        }
                        
                    });
                }
            });

            return temporalFiles;
        } catch (err) {
            this.logger.error(`There was an error while refreshing any Skill from the Assistant:\n${ err }`);
            return new Map();
        }
    }

    private async executeLudownParse(culture: string, executionModelByCulture: [Map<string, string>]): Promise<void> {
        const ludownParseCommand: string[] = ['ludown', 'parse', 'toluis'];
        try {
            for (const element of executionModelByCulture) {
                const luisApp: string = element.get('luisApp') as string;
                const luisFile: string = element.get('luisFile') as string;
                const luisFilePath: string = element.get('luisFilePath') as string;
                // Parse LU file
                const ludownParseCommandArguments: string[] = ['--in', '--luis_culture', '--out_folder', '--out'];
                ludownParseCommandArguments.forEach((argument: string): void => {
                    const argumentValue: string = element.get(argument) as string;
                    ludownParseCommand.push(...[argument, argumentValue]);
                });
                await this.runCommand(ludownParseCommand, `Parsing ${ culture } ${ luisApp } LU file`);
                if (!existsSync(luisFilePath)) {
                    throw new Error(`Path to ${ luisFile } (${ luisFilePath }) leads to a nonexistent file.`);
                }
            }
        } catch (err) {
            throw new Error(`There was an error in the ludown parse command:\nCommand: ${ ludownParseCommand.join(' ') }\n${ err }`);
        }
    }

    public async refreshSkill(): Promise<boolean> {
        try {
            const temporalFiles: Map<string, string[]> = await this.getTemporalFiles();

            if(temporalFiles.size > 0){
                await this.processTemporalFiles(temporalFiles);
            }
            
            await this.updateModel();
                
            this.logger.success('Successfully refreshed Dispatch model');

            await deleteFiles(this.tempFiles);
            this.logger.warning(
                'You need to re-publish your Virtual Assistant in order to have these changes available for Azure based testing');

            return true;
        } catch (err) {
            this.logger.error(`There was an error while refreshing any Skill from the Assistant:\n${ err }`);

            return false;
        }
    }
}
