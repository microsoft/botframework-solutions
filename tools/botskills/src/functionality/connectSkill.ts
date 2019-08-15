/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync, writeFileSync } from 'fs';
import { isAbsolute, join, resolve } from 'path';
import { get } from 'request-promise-native';
import { ConsoleLogger, ILogger } from '../logger';
import {
    IAction,
    ICognitiveModelFile,
    IConnectConfiguration,
    IRefreshConfiguration,
    ISkillFile,
    ISkillManifest,
    IUtteranceSource
} from '../models';
import { AuthenticationUtils, ChildProcessUtils, getDispatchNames, isValidCultures, wrapPathWithQuotes } from '../utils';
import { RefreshSkill } from './refreshSkill';

export class ConnectSkill {
    private readonly authenticationUtils: AuthenticationUtils;
    private readonly childProcessUtils: ChildProcessUtils;
    private readonly configuration: IConnectConfiguration;
    private readonly logger: ILogger;

    public constructor(configuration: IConnectConfiguration, logger?: ILogger) {
        this.configuration = configuration;
        this.logger = logger || new ConsoleLogger();
        this.authenticationUtils = new AuthenticationUtils();
        this.childProcessUtils = new ChildProcessUtils();
    }

    private getExecutionModel(
        luisApp: string,
        culture: string,
        intentName: string,
        dispatchName: string): Map<string, string> {
        const luFile: string = `${luisApp}.lu`;
        const luisFile: string = `${luisApp}.luis`;
        const luFilePath: string = join(this.configuration.luisFolder, culture, luFile);
        const luisFolderPath: string = join(this.configuration.luisFolder, culture);
        const luisFilePath: string = join(luisFolderPath, luisFile);
        const dispatchFile: string = `${dispatchName}.dispatch`;
        const dispatchFolderPath: string = join(this.configuration.dispatchFolder, culture);
        const dispatchFilePath: string = join(dispatchFolderPath, dispatchFile);

        // Validate 'ludown' arguments
        if (!existsSync(this.configuration.luisFolder)) {
            throw new Error(`Path to the LUIS folder (${this.configuration.luisFolder}) leads to a nonexistent folder.
Remember to use the argument '--luisFolder' for your Skill's LUIS folder.`);
        } else if (!existsSync(luFilePath)) {
            throw new Error(`Path to the ${luisApp}.lu file leads to a nonexistent file.
Make sure your Skill's .lu file's name matches your Skill's manifest id`);
        }

        // Validate 'dispatch add' arguments
        if (!existsSync(dispatchFolderPath)) {
            throw new Error(
                `Path to the Dispatch folder (${dispatchFolderPath}) leads to a nonexistent folder.
Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        } else if (!existsSync(dispatchFilePath)) {
            throw new Error(`Path to the ${dispatchFile} file leads to a nonexistent file.`);
        }

        // tslint:disable:no-backbone-get-set-outside-model
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
        executionModelMap.set('--dataFolder', dispatchFolderPath);
        executionModelMap.set('--dispatch', dispatchFilePath);
        // tslint:enable:no-backbone-get-set-outside-model

        return executionModelMap;
    }

    private getLocalManifest(): ISkillManifest {
        const manifestPath: string = this.configuration.localManifest;
        const skillManifestPath: string = isAbsolute(manifestPath) ? manifestPath : join(resolve('./'), manifestPath);

        if (!existsSync(skillManifestPath)) {
            throw new Error(`The 'localManifest' argument leads to a non-existing file.
Please make sure to provide a valid path to your Skill manifest using the '--localManifest' argument.`);
        }

        // eslint-disable-next-line @typescript-eslint/tslint/config
        return JSON.parse(readFileSync(skillManifestPath, 'UTF8'));
    }

    private validateManifestSchema(skillManifest: ISkillManifest): void {
        if (!skillManifest.name) {
            this.logger.error(`Missing property 'name' of the manifest`);
        }
        if (!skillManifest.id) {
            this.logger.error(`Missing property 'id' of the manifest`);
        } else if (skillManifest.id.match(/^\d|[^\w]/g) !== null) {
            // tslint:disable-next-line:max-line-length
            this.logger.error(`The 'id' of the manifest contains some characters not allowed. Make sure the 'id' contains only letters, numbers and underscores, but doesn't start with number.`);
        }
        if (!skillManifest.endpoint) {
            this.logger.error(`Missing property 'endpoint' of the manifest`);
        }
        if (skillManifest.authenticationConnections === undefined) {
            this.logger.error(`Missing property 'authenticationConnections' of the manifest`);
        }
        if (skillManifest.actions === undefined || skillManifest.actions[0] === undefined) {
            this.logger.error(`Missing property 'actions' of the manifest`);
        }
    }

    private async runCommand(command: string[], description: string): Promise<string> {
        this.logger.command(description, command.join(' '));
        const cmd: string = command[0];
        const commandArgs: string[] = command.slice(1)
            .filter((arg: string): string => arg);

        try {
            return await this.childProcessUtils.execute(cmd, commandArgs);
        } catch (err) {
            throw new Error(`The execution of the ${cmd} command failed with the following error:\n${err}`);
        }
    }

    private async getManifest(): Promise<ISkillManifest> {

        return this.configuration.localManifest
            ? this.getLocalManifest()
            : this.getRemoteManifest();
    }

    private async getRemoteManifest(): Promise<ISkillManifest> {
        try {
            return get({
                uri: this.configuration.remoteManifest,
                json: true
            });
        } catch (err) {
            throw new Error(`There was a problem while getting the remote manifest:\n${err}`);
        }
    }

    private async validateCultures(cognitiveModelsFile: ICognitiveModelFile, luisDictionary: Map<string, string[]>): Promise<void> {
        const dispatchLanguages: string [] = Object.keys(cognitiveModelsFile.cognitiveModels)
            .filter((key: string): boolean => cognitiveModelsFile.cognitiveModels[key].dispatchModel !== undefined);
        const manifestLanguages: string[] = Array.from(luisDictionary.keys());
        const availableCultures: string[] = dispatchLanguages.filter((lang: string): boolean => manifestLanguages.includes(lang));
        if (!isValidCultures(availableCultures, this.configuration.languages)) {
            throw new Error(`Some of the cultures provided to connect from the Skill are not available or aren't supported by your VA.
Make sure you have a Dispatch for the cultures you are trying to connect, and that your Skill has a LUIS model for that culture`);
        }
    }

    private async processManifest(manifest: ISkillManifest): Promise<Map<string, string[]>> {

        return manifest.actions.filter((action: IAction): IUtteranceSource[] =>
            action.definition.triggers.utteranceSources)
            .reduce((acc: IUtteranceSource[], val: IAction): IUtteranceSource[] => acc.concat(val.definition.triggers.utteranceSources), [])
            .reduce(
                (acc: Map<string, string[]>, val: IUtteranceSource): Map<string, string[]> => {
                    const luisApps: string[] = val.source.map((v: string): string => v.split('#')[0]);
                    if (acc.has(val.locale)) {
                        const previous: string[] = acc.get(val.locale) || [];
                        const filteredluisApps: string[] = [...new Set(luisApps.concat(previous))];
                        acc.set(val.locale, filteredluisApps);
                    } else {
                        const filteredluisApps: string[] = [...new Set(luisApps)];
                        acc.set(val.locale, filteredluisApps);
                    }

                    return acc;
                },
                new Map());
    }

    private async executeLudownParse(culture: string, executionModelByCulture: Map<string, string>): Promise<void> {
        const ludownParseCommand: string[] = ['ludown', 'parse', 'toluis'];
        try {
            // tslint:disable:no-backbone-get-set-outside-model
            const luisApp: string = <string> executionModelByCulture.get('luisApp');
            const luisFile: string = <string> executionModelByCulture.get('luisFile');
            const luisFilePath: string = <string> executionModelByCulture.get('luisFilePath');
            // Parse LU file
            const ludownParseCommandArguments: string[] = ['--in', '--luis_culture', '--out_folder', '--out'];
            ludownParseCommandArguments.forEach((argument: string): void => {
                const argumentValue: string = <string> executionModelByCulture.get(argument);
                ludownParseCommand.push(...[argument, argumentValue]);
            });
            await this.runCommand(ludownParseCommand, `Parsing ${culture} ${luisApp} LU file`);
            // tslint:enable:no-backbone-get-set-outside-model
            if (!existsSync(luisFilePath)) {
                // tslint:disable-next-line: max-line-length
                throw new Error(`Path to ${luisFile} (${luisFilePath}) leads to a nonexistent file.`);
            }
        } catch (err) {
            throw new Error(`There was an error in the ludown parse command:\nCommand: ${ludownParseCommand.join(' ')}\n${err}`);
        }
    }

    private async executeDispatchAdd(culture: string, executionModelByCulture: Map<string, string>): Promise<void> {
        const dispatchAddCommand: string[] = ['dispatch', 'add'];
        try {
            // tslint:disable-next-line:no-backbone-get-set-outside-model
            const luisApp: string = <string> executionModelByCulture.get('luisApp');
            // Update Dispatch file
            const dispatchAddCommandArguments: string[] = ['--type', '--name', '--filePath', '--intentName', '--dataFolder', '--dispatch'];
            dispatchAddCommandArguments.forEach((argument: string): void => {
                const argumentValue: string = <string> executionModelByCulture.get(argument);
                dispatchAddCommand.push(...[argument, argumentValue]);
            });
            await this.runCommand(dispatchAddCommand, `Executing dispatch add for the ${culture} ${luisApp} LU file`);
        } catch (err) {
            throw new Error(`There was an error in the dispatch add command:\nCommand: ${dispatchAddCommand.join(' ')}\n${err}`);
        }
    }

    private async executeRefresh(): Promise<void> {
        const refreshConfiguration: IRefreshConfiguration = { ...{}, ...this.configuration };
        const refreshSkill: RefreshSkill = new RefreshSkill(refreshConfiguration, this.logger);
        if (!await refreshSkill.refreshSkill()) {
            throw new Error(`There was an error while refreshing the Dispatch model.`);
        }
    }

    private async updateModel(luisDictionary: Map<string, string[]>, intentName: string): Promise<void> {
        try {
            const filteredLuisDictionary: [string, string[]][] = Array.from(luisDictionary.entries())
                .filter((item: [string, string[]]): boolean => this.configuration.languages.includes(item[0]));
            this.logger.message('Adding skill to Dispatch');

            // eslint-disable-next-line @typescript-eslint/tslint/config
            const cognitiveModelsFile: ICognitiveModelFile = JSON.parse(readFileSync(this.configuration.cognitiveModelsFile, 'UTF8'));
            const dispatchNames: Map<string, string> = getDispatchNames(cognitiveModelsFile);

            const executionsModelMap: Map<string, Map<string, string>> = new Map();
            filteredLuisDictionary.map((item: [string, string[]]): void => {
                const luisCulture: string = item[0];
                const filteredluisApps: string[] = item[1];
                const dispatchName: string = <string> dispatchNames.get(luisCulture);
                filteredluisApps.map((luisApp: string): void => {
                    executionsModelMap.set(luisCulture, this.getExecutionModel(luisApp, luisCulture, intentName, dispatchName));
                });
            });

            await Promise.all(Array.from(executionsModelMap.entries())
                .map(async (item: [string, Map<string, string>]): Promise<void> => {
                    const culture: string = item[0];
                    const executionModelByCulture: Map<string, string> = item[1];
                    await this.executeLudownParse(culture, executionModelByCulture);
                    await this.executeDispatchAdd(culture, executionModelByCulture);
                }));

            // Check if it is necessary to refresh the skill
            if (!this.configuration.noRefresh) {
                await this.executeRefresh();
            } else {
                this.logger.warning(`Run 'botskills refresh --${this.configuration.lgLanguage}' command to refresh your connected skills`);
            }
        } catch (err) {
            throw new Error(`An error ocurred while updating the Dispatch model:\n${err}`);
        }
    }

    public async connectSkill(): Promise<boolean> {
        try {
            // Validate if no manifest path or URL was passed
            if (!this.configuration.localManifest && !this.configuration.remoteManifest) {
                throw new Error(`Either the 'localManifest' or 'remoteManifest' argument should be passed.`);
            }

            // Validate if cognitiveModels files doesn't exist
            if (!existsSync(this.configuration.cognitiveModelsFile)) {
                throw new Error(`Could not find the cognitiveModels file (${
                    this.configuration.cognitiveModelsFile}). Please provide the '--cognitiveModelsFile' argument.`);
            }
            // Take cognitiveModels
            // eslint-disable-next-line @typescript-eslint/tslint/config
            const cognitiveModelsFile: ICognitiveModelFile = JSON.parse(readFileSync(this.configuration.cognitiveModelsFile, 'UTF8'));
            // Take skillManifest
            const skillManifest: ISkillManifest = await this.getManifest();
            // Manifest schema validation
            this.validateManifestSchema(skillManifest);

            if (this.logger.isError) {

                return false;
            }
            // End of manifest schema validation

            // Take VA Skills configurations
            // eslint-disable-next-line @typescript-eslint/tslint/config
            const assistantSkillsFile: ISkillFile = JSON.parse(readFileSync(this.configuration.skillsFile, 'UTF8'));
            const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills !== undefined ? assistantSkillsFile.skills : [];

            // Check if the skill is already connected to the assistant
            if (assistantSkills.find((assistantSkill: ISkillManifest): boolean => assistantSkill.id === skillManifest.id)) {
                this.logger.warning(`The skill '${skillManifest.name}' is already registered.`);

                return false;
            }

            // Process the manifest to get the intents and cultures of each intent
            const luisDictionary: Map<string, string[]> = await this.processManifest(skillManifest);
            // Validate cultures
            await this.validateCultures(cognitiveModelsFile, luisDictionary);
            // Updating Dispatch
            this.logger.message('Updating Dispatch');
            await this.updateModel(luisDictionary, skillManifest.id);
            // Adding the skill manifest to the assistant skills array
            this.logger.message(`Appending '${skillManifest.name}' manifest to your assistant's skills configuration file.`);
            assistantSkills.push(skillManifest);
            // Updating the assistant skills file's skills property with the assistant skills array
            assistantSkillsFile.skills = assistantSkills;
            // Writing (and overriding) the assistant skills file
            writeFileSync(this.configuration.skillsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
            this.logger.success(`Successfully appended '${skillManifest.name}' manifest to your assistant's skills configuration file!`);
            // Configuring bot auth settings
            this.logger.message('Configuring bot auth settings');
            await this.authenticationUtils.authenticate(this.configuration, skillManifest, this.logger);

            return true;
        } catch (err) {
            this.logger.error(`There was an error while connecting the Skill to the Assistant:\n${err}`);

            return false;
        }
    }
}
