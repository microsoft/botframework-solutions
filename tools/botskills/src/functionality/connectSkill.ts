/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'fs';
import { isAbsolute, join, resolve } from 'path';
import { get } from 'request-promise-native';
import { ConsoleLogger, ILogger } from '../logger';
import {
    IAction,
    ICognitiveModel,
    IConnectConfiguration,
    IRefreshConfiguration,
    ISkillManifestV1,
    IUtteranceSource,
    ISkillManifestV2,
    IAppSetting,
    ISkill,
    IModel,
    IEndpoint
} from '../models';
import { ChildProcessUtils, getDispatchNames, isValidCultures, wrapPathWithQuotes, manifestV1Validation, manifestV2Validation } from '../utils';
import { RefreshSkill } from './refreshSkill';

enum manifestVersion {
    V1 = 'V1',
    V2 = 'V2',
    none = 'none'
}

export class ConnectSkill {
    private readonly childProcessUtils: ChildProcessUtils;
    private readonly configuration: IConnectConfiguration;
    private readonly logger: ILogger;
    private manifestVersion: manifestVersion | undefined;
    private skillManifest: ISkillManifestV2 | undefined;
    private skillManifestValidated: manifestVersion = manifestVersion.none;

    public constructor(configuration: IConnectConfiguration, logger?: ILogger) {
        this.configuration = configuration;
        this.logger = logger || new ConsoleLogger();
        this.childProcessUtils = new ChildProcessUtils();
    }

    private async getExecutionModel(
        luisApp: string,
        culture: string,
        intentName: string,
        dispatchName: string): Promise<Map<string, string>> {

        let luFile = '';
        let luisFile = '';
        let luFilePath = '';
        let luisFolderPath: string = join(this.configuration.luisFolder, culture);
        let luisFilePath = '';
        let dispatchFile = '';
        let dispatchFolderPath = '';
        let dispatchFilePath = '';
        let allowedIntents: string[] = [];
        let useAllIntents: boolean = false;

        if (this.manifestVersion == manifestVersion.V1)
        {
            luFile = `${ luisApp }.lu`;
            luisFile = `${ luisApp }.luis`;
            luFilePath = join(this.configuration.luisFolder, culture, luFile);
            luisFilePath = join(luisFolderPath, luisFile);
            dispatchFile = `${ dispatchName }.dispatch`;
            dispatchFolderPath = join(this.configuration.dispatchFolder, culture);
            dispatchFilePath = join(dispatchFolderPath, dispatchFile);
        }
        else {

            if (this.skillManifest){

                const model: IModel = {id: '', name: '', contentType: '', url: '', description: ''};
                const entries = Object.entries(this.skillManifest?.dispatchModels.languages);
                const currentLocaleApps = entries.find((entry: [string, IModel[]]): boolean => entry[0] === culture) || [model];
                const localeApps: IModel[] = currentLocaleApps[1];
                const currentApp: IModel = localeApps.find((model: IModel): boolean => model.id === luisApp) || model;
                allowedIntents = Object.keys(this.skillManifest?.dispatchModels.intents);
                useAllIntents = allowedIntents.some(e => e === '*');
                
                if (currentApp.url.startsWith('file')) {
                    luFilePath = currentApp.url.split('file://')[1];
                    if(!existsSync(luFilePath)) {
                        luFile = luFilePath;
                        luisFile = `${ luFile.toLowerCase() }is`;
                        luFilePath = join(this.configuration.luisFolder, culture, luFile);
                    }
                }
                else if (currentApp.url.startsWith('http')) {
                    try {
                        const remoteLuFile = await this.getRemoteLu(currentApp.url);
                        let luisAppName: string = currentApp.url.split('/').reverse()[0];

                        const luPath = join(this.configuration.luisFolder, culture, luisAppName.endsWith('.lu') ? luisAppName : luisAppName + '.lu');
                        this.verifyLuisFolder(culture);
                        writeFileSync(luPath, remoteLuFile);
                        luFilePath = luPath;
                    } catch (error) {
                        console.log(error);
                    }
                }
                else {
                    luFilePath = currentApp.url;
                }
                
                if(!existsSync(luFilePath)) {
                    throw new Error(`Path to the LU file (${ luFilePath }) leads to a nonexistent file.`);
                }

                if (luFile.trim.length === 0) {
                    luFile = luFilePath.split('\\').reverse()[0];
                    luisFile = `${ luFile.toLowerCase() }is`;
                }
                luisFilePath = join(luisFolderPath, luisFile);
                dispatchFile = `${ dispatchName }.dispatch`;
                dispatchFolderPath = join(this.configuration.dispatchFolder, culture);
                dispatchFilePath = join(dispatchFolderPath, dispatchFile);
            }
        }

        // Validate 'bf luis:convert' arguments
        if (!existsSync(this.configuration.luisFolder)) {
            throw new Error(`Path to the LUIS folder (${ this.configuration.luisFolder }) leads to a nonexistent folder.
Remember to use the argument '--luisFolder' for your Skill's LUIS folder.`);
        } else if (!existsSync(luFilePath)) {
            throw new Error(`Path to the ${ luFile } file leads to a nonexistent file.
Make sure your Skill's .lu file's name matches your Skill's manifest id`);
        }
        
        // Validate 'dispatch add' arguments
        if (!existsSync(dispatchFolderPath)) {
            throw new Error(
                `Path to the Dispatch folder (${ dispatchFolderPath }) leads to a nonexistent folder.
Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        } else if (!existsSync(dispatchFilePath)) {
            throw new Error(`Path to the ${ dispatchFile } file leads to a nonexistent file.`);
        }

        const executionModelMap: Map<string, string> = new Map();
        executionModelMap.set('luisApp', luisApp);
        executionModelMap.set('luisFile', luisFile);
        executionModelMap.set('luisFilePath', luisFilePath);
        executionModelMap.set('--in', wrapPathWithQuotes(luFilePath));
        executionModelMap.set('--culture', culture);
        executionModelMap.set('--out', luisFilePath);
        executionModelMap.set('--type', 'file');
        executionModelMap.set('--name', intentName);
        executionModelMap.set('--filePath', luisFilePath);
        executionModelMap.set('--intentName', intentName);
        executionModelMap.set('--dataFolder', dispatchFolderPath);
        executionModelMap.set('--dispatch', dispatchFilePath);

        if (useAllIntents && allowedIntents.length > 1) {
            this.logger.warning("Found intent with name '*'. Adding all intents.");
        }
        
        if (!useAllIntents && allowedIntents.length > 0) {
            executionModelMap.set('--includedIntents', allowedIntents.join(','));
        }

        return executionModelMap;
    }

    private validateManifestSchema(skillManifest: ISkillManifestV1 | ISkillManifestV2): manifestVersion {

        const skillManifestV1Validation = skillManifest as ISkillManifestV1;
        const skillManifestV2Validation = skillManifest as ISkillManifestV2;

        const skillManifestVersion: string | undefined = skillManifestV1Validation.id ? 
            manifestVersion.V1 : skillManifestV2Validation.$id ?
                manifestVersion.V2 : undefined;
        
        let validVersion: manifestVersion = manifestVersion.none;
        switch (skillManifestVersion) {
            case manifestVersion.V1: {
                manifestV1Validation(skillManifest as ISkillManifestV1, this.logger);
                if (!this.logger.isError)
                {
                    validVersion = manifestVersion.V1;
                    break;
                }
                throw new Error('Your Skill Manifest is not compatible. Please note that the minimum supported manifest version is 2.1.');
            }
            case manifestVersion.V2: {
                manifestV2Validation(skillManifest as ISkillManifestV2, this.logger, this.configuration.endpointName);
                if (!this.logger.isError)
                {
                    validVersion = manifestVersion.V2;
                    break;
                }
                throw new Error('Your Skill Manifest is not compatible. Please note that the minimum supported manifest version is 2.1.');
            }
            case undefined: {
                throw new Error('Your Skill Manifest is not compatible. Please note that the minimum supported manifest version is 2.1.');
            }
        }

        return validVersion;
        
    }

    private async runCommand(command: string[], description: string): Promise<string> {
        this.logger.command(description, command.join(' '));
        const cmd: string = command[0];
        const commandArgs: string[] = command.slice(1)
            .filter((arg: string): string => arg);

        try {
            return await this.childProcessUtils.execute(cmd, commandArgs);
        } catch (err) {
            throw new Error(`The execution of the ${ cmd } command failed with the following error:\n${ err }`);
        }
    }

    private async getRemoteLu(path: string): Promise<string> {
        try {
            return get({
                uri: path
            });
        } catch (err) {
            throw new Error(`There was a problem while getting the remote lu file:\n${ err }`);
        }
    }

    private async getManifest(): Promise<ISkillManifestV1 | ISkillManifestV2> {

        return this.configuration.localManifest
            ? this.getLocalManifest()
            : this.getRemoteManifest();
    }

    private async getRemoteManifest(): Promise<ISkillManifestV1 | ISkillManifestV2> {
        try {
            return get({
                uri: this.configuration.remoteManifest,
                json: true
            });
        } catch (err) {
            throw new Error(`There was a problem while getting the remote manifest:\n${ err }`);
        }
    }

    private getLocalManifest(): ISkillManifestV1 | ISkillManifestV2 {
        const manifestPath: string = this.configuration.localManifest;
        const skillManifestPath: string = isAbsolute(manifestPath) ? manifestPath : join(resolve('./'), manifestPath);

        if (!existsSync(skillManifestPath)) {
            throw new Error(`The 'localManifest' argument leads to a non-existing file.
Please make sure to provide a valid path to your Skill manifest using the '--localManifest' argument.`);
        }

        return JSON.parse(readFileSync(skillManifestPath, 'UTF8'));
    }

    private verifyLuisFolder(culture: string): void {
        if (!existsSync(this.configuration.luisFolder)){
            mkdirSync(this.configuration.luisFolder);
        }

        if (!existsSync(join(this.configuration.luisFolder, culture))) {
            mkdirSync(join(this.configuration.luisFolder, culture));
        }
    }

    private async validateCultures(cognitiveModelsFile: ICognitiveModel, luisDictionary: Map<string, string[]>): Promise<void> {
        const dispatchLanguages: string [] = Object.keys(cognitiveModelsFile.cognitiveModels)
            .filter((key: string): boolean => cognitiveModelsFile.cognitiveModels[key].dispatchModel !== undefined);
        const manifestLanguages: string[] = Array.from(luisDictionary.keys());
        const availableCultures: string[] = dispatchLanguages.filter((lang: string): boolean => manifestLanguages.includes(lang));
        if (!isValidCultures(availableCultures, this.configuration.languages)) {
            throw new Error(`Some of the cultures provided to connect from the Skill are not available or aren't supported by your VA.
Make sure you have a Dispatch for the cultures you are trying to connect, and that your Skill has a LUIS model for that culture`);
        }
    }

    private async processManifestV1(manifest: ISkillManifestV1): Promise<Map<string, string[]>> {

        return manifest.actions.filter((action: IAction): IUtteranceSource[] =>
            action.definition.triggers.utteranceSources).reduce((acc: IUtteranceSource[], val: IAction): IUtteranceSource[] => acc.concat(val.definition.triggers.utteranceSources), [])
            .reduce((acc: Map<string, string[]>, val: IUtteranceSource): Map<string, string[]> => {
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

    private async executeLuisConvert(culture: string, executionModelByCulture: Map<string, string>): Promise<void> {
        const luisConvertCommand: string[] = ['bf', 'luis:convert'];
        try {
            const luisApp: string = executionModelByCulture.get('luisApp') as string;
            const luisFile: string = executionModelByCulture.get('luisFile') as string;
            const luisFilePath: string = executionModelByCulture.get('luisFilePath') as string;
            // Parse LU file
            const luisConvertCommandArguments: string[] = ['--in', '--culture', '--out', '--name'];
            luisConvertCommandArguments.forEach((argument: string): void => {
                const argumentValue: string = executionModelByCulture.get(argument) as string;
                luisConvertCommand.push(...[argument, argumentValue]);
            });
            await this.runCommand(luisConvertCommand, `Parsing ${ culture } ${ luisApp } LU file`);
            if (!existsSync(luisFilePath)) {
                throw new Error(`Path to ${ luisFile } (${ luisFilePath }) leads to a nonexistent file.`);
            }
        } catch (err) {
            throw new Error(`There was an error in the bf luis:convert command:\nCommand: ${ luisConvertCommand.join(' ') }\n${ err }`);
        }
    }

    private async executeDispatchAdd(culture: string, executionModelByCulture: Map<string, string>): Promise<void> {
        const dispatchAddCommand: string[] = ['dispatch', 'add'];
        try {
            const luisApp: string = executionModelByCulture.get('luisApp') as string;
            // Update Dispatch file
            const dispatchAddCommandArguments: string[] = ['--type', '--name', '--filePath', '--intentName', '--dataFolder', '--dispatch']; 
            
            // In cause of using specific intentsm we pass them to Dispatch
            if (executionModelByCulture.has('--includedIntents')) {
                dispatchAddCommandArguments.push('--includedIntents');
            }

            dispatchAddCommandArguments.forEach((argument: string): void => {
                const argumentValue: string = executionModelByCulture.get(argument) as string;
                dispatchAddCommand.push(...[argument, argumentValue]);
            });
            await this.runCommand(dispatchAddCommand, `Executing dispatch add for the ${ culture } ${ luisApp } LU file`);
        } catch (err) {
            throw new Error(`There was an error in the dispatch add command:\nCommand: ${ dispatchAddCommand.join(' ') }\n${ err }`);
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

            const cognitiveModelsFile: ICognitiveModel = JSON.parse(readFileSync(this.configuration.cognitiveModelsFile, 'UTF8'));
            const dispatchNames: Map<string, string> = getDispatchNames(cognitiveModelsFile);

            const executionsModelMap: Map<string, Map<string, string>> = new Map();
            await Promise.all(filteredLuisDictionary.map( async (item: [string, string[]]): Promise<void> => {
                const luisCulture: string = item[0];
                const filteredluisApps: string[] = item[1];
                const dispatchName: string = dispatchNames.get(luisCulture) as string;
                await Promise.all(filteredluisApps.map(async (luisApp: string): Promise<void> => {
                    executionsModelMap.set(luisCulture, await this.getExecutionModel(luisApp, luisCulture, intentName, dispatchName));
                }));
            }));

            await Promise.all(Array.from(executionsModelMap.entries())
                .map(async (item: [string, Map<string, string>]): Promise<void> => {
                    const culture: string = item[0];
                    const executionModelByCulture: Map<string, string> = item[1];
                    await this.executeLuisConvert(culture, executionModelByCulture);
                    await this.executeDispatchAdd(culture, executionModelByCulture);
                }));

            // Check if it is necessary to refresh the skill
            if (!this.configuration.noRefresh) {
                await this.executeRefresh();
            } else {
                this.logger.warning(`Run 'botskills refresh --${ this.configuration.lgLanguage }' command to refresh your connected skills`);
            }
        } catch (err) {
            throw new Error(`An error ocurred while updating the Dispatch model:\n${ err }`);
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
                    this.configuration.cognitiveModelsFile }). Please provide the '--cognitiveModelsFile' argument.`);
            }
            
            // Take cognitiveModels
            const cognitiveModelsFile: ICognitiveModel = JSON.parse(readFileSync(this.configuration.cognitiveModelsFile, 'UTF8'));
            // Take skillManifest
            const skillManifest: ISkillManifestV1 | ISkillManifestV2 = await this.getManifest();
            // Manifest schema validation
            this.skillManifestValidated = this.validateManifestSchema(skillManifest);
            // End of manifest schema validation

            switch (this.skillManifestValidated) {
                case manifestVersion.V1: {
                    this.manifestVersion = manifestVersion.V1;
                    await this.connectSkillManifestV1(cognitiveModelsFile, skillManifest as ISkillManifestV1);
                    break;
                }
                case manifestVersion.V2: {
                    this.manifestVersion = manifestVersion.V2;
                    await this.connectSkillManifestV2(cognitiveModelsFile, skillManifest as ISkillManifestV2);
                    break;
                }
            }

            return true;
           
        } catch (err) {
            this.logger.error(`There was an error while connecting the Skill to the Assistant:\n${ err }`);
            return false;
        }
    }

    private AddSkill(assistantSkillsFile: IAppSetting, assistantSkills: ISkill[], skill: ISkillManifestV1 | ISkillManifestV2): void {

        if (this.skillManifestValidated == manifestVersion.V1) {
            const skillManifestV1: ISkillManifestV1 = skill as ISkillManifestV1;
            assistantSkills.push({
                Id: skillManifestV1.id,
                AppId: skillManifestV1.msaAppId,
                SkillEndpoint: skillManifestV1.endpoint,
                Name: skillManifestV1.name
            });
            assistantSkillsFile.BotFrameworkSkills = assistantSkills;
        }

        if (this.skillManifestValidated == manifestVersion.V2) {
            const skillManifestV2: ISkillManifestV2 = skill as ISkillManifestV2;
            const endpoint: IEndpoint = skillManifestV2.endpoints.find((endpoint: IEndpoint): boolean => endpoint.name === this.configuration.endpointName) 
            || skillManifestV2.endpoints[0];
            
            assistantSkills.push({
                Id: skillManifestV2.$id,
                AppId: endpoint.msAppId,
                SkillEndpoint: endpoint.endpointUrl,
                Name: skillManifestV2.name,
            });
            assistantSkillsFile.BotFrameworkSkills = assistantSkills;
        }
        
        
        if (assistantSkillsFile.SkillHostEndpoint === undefined || assistantSkillsFile.SkillHostEndpoint.trim().length === 0) {
            assistantSkillsFile.SkillHostEndpoint = `https://${ this.configuration.botName }.azurewebsites.net/api/skills`;
        }
        writeFileSync(this.configuration.appSettingsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
    }

    private async connectSkillManifestV1(cognitiveModelsFile: ICognitiveModel, skillManifest: ISkillManifestV1): Promise<void> {
        try {
            // Take VA Skills configurations
            const assistantSkillsFile: IAppSetting = JSON.parse(readFileSync(this.configuration.appSettingsFile, 'UTF8'));
            const assistantSkills: ISkill[] = assistantSkillsFile.BotFrameworkSkills !== undefined ? assistantSkillsFile.BotFrameworkSkills : [];

            // Check if the skill is already connected to the assistant
            if (assistantSkills.find((assistantSkill: ISkill): boolean => assistantSkill.Id === skillManifest.id)) {
                this.logger.warning(`The skill '${ skillManifest.name }' is already registered.`);
                return;
            }

            // Process the manifest to get the intents and cultures of each intent
            const luisDictionary: Map<string, string[]> = await this.processManifestV1(skillManifest);
            // Validate cultures
            await this.validateCultures(cognitiveModelsFile, luisDictionary);
            // Updating Dispatch
            this.logger.message('Updating Dispatch');
            await this.updateModel(luisDictionary, skillManifest.id);
            // Adding the skill manifest to the assistant skills array
            this.logger.message(`Appending '${ skillManifest.name }' manifest to your assistant's skills configuration file.`);
            // Updating the assistant skills file's skills property with the assistant skills array
            // Writing (and overriding) the assistant skills file
            //writeFileSync(this.configuration.skillsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
            this.AddSkill(assistantSkillsFile, assistantSkills, skillManifest);
            this.logger.success(`Successfully appended '${ skillManifest.name }' manifest to your assistant's skills configuration file!`);
            // Configuring bot auth settings
            //this.logger.message('Configuring bot auth settings');
            //await this.authenticationUtils.authenticate(this.configuration, skillManifest, this.logger);
        } catch (err) {
            this.logger.error(`There was an error while connecting the Skill to the Assistant:\n${ err }`);
        }
    }

    private async connectSkillManifestV2(cognitiveModelsFile: ICognitiveModel, skillManifest: ISkillManifestV2): Promise<void> {
        try {
            // Take VA Skills configurations
            const assistantSkillsFile: IAppSetting = JSON.parse(readFileSync(this.configuration.appSettingsFile, 'UTF8'));
            const assistantSkills: ISkill[] = assistantSkillsFile.BotFrameworkSkills !== undefined ? assistantSkillsFile.BotFrameworkSkills : [];

            // Check if the skill is already connected to the assistant
            if (assistantSkills.find((assistantSkill: ISkill): boolean => assistantSkill.Id === skillManifest.$id)) {
                this.logger.warning(`The skill '${ skillManifest.name }' is already registered.`);
                return;
            }
            this.skillManifest = skillManifest;
            const luisDictionary: Map<string, string[]> = await this.processManifestV2(skillManifest);

            // Validate cultures
            await this.validateCultures(cognitiveModelsFile, luisDictionary);
            // Updating Dispatch
            this.logger.message('Updating Dispatch');
            await this.updateModel(luisDictionary, skillManifest.$id);
            // Adding the skill manifest to the assistant skills array
            this.logger.message(`Appending '${ skillManifest.name }' manifest to your assistant's skills configuration file.`);
            // Updating the assistant skills file's skills property with the assistant skills array
            // Writing (and overriding) the assistant skills file
            //writeFileSync(this.configuration.skillsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
            this.AddSkill(assistantSkillsFile, assistantSkills, skillManifest);
            this.logger.success(`Successfully appended '${ skillManifest.name }' manifest to your assistant's skills configuration file!`);
            // Configuring bot auth settings
            //this.logger.message('Configuring bot auth settings');
            //await this.authenticationUtils.authenticate(this.configuration, skillManifest, this.logger);
            
            return;
        } catch (err) {
            this.logger.error(`There was an error while connecting the Skill to the Assistant:\n${ err }`);
        }
    }

    private async processManifestV2(manifest: ISkillManifestV2): Promise<Map<string, string[]>> {
        const acc: Map<string, string[]> = new Map();
        const entries = Object.entries(manifest.dispatchModels.languages);

        entries.forEach(([locale, value]): void => {
            const luisApps: string[] = [];
            value.forEach((model: IModel): void => {
                luisApps.push(model.id);
            });
        
            const filteredluisApps: string[] = [...new Set(luisApps)];
            acc.set(locale, filteredluisApps);
        });

        return acc;
    }
}
