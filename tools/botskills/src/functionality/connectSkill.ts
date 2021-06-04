/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'fs';
import { join, parse } from 'path';
import { get } from 'request-promise-native';
import { ConsoleLogger, ILogger } from '../logger';
import {
    ICognitiveModel,
    IConnectConfiguration,
    IRefreshConfiguration,
    IAppSetting,
    ISkill,
    IModel
} from '../models';
import { ChildProcessUtils, getDispatchNames, isValidCultures, wrapPathWithQuotes, isCloudGovernment, ManifestUtils, libraries, validateLibrary, sanitizeAppSettingsProperties  } from '../utils';
import { RefreshSkill } from './refreshSkill';
import { IManifest } from '../models/manifest';
import { EOL } from 'os';

export class ConnectSkill {
    private readonly childProcessUtils: ChildProcessUtils;
    private readonly manifestUtils: ManifestUtils;
    private readonly configuration: IConnectConfiguration;
    private readonly logger: ILogger;
    private manifest: IManifest | undefined;

    public constructor(configuration: IConnectConfiguration, logger?: ILogger) {
        this.configuration = configuration;
        this.logger = logger || new ConsoleLogger();
        this.childProcessUtils = new ChildProcessUtils();
        this.manifestUtils = new ManifestUtils();
    }

    private async getExecutionModel(
        luisApp: string,
        culture: string,
        intentName: string,
        dispatchName: string): Promise<Map<string, string>> {

        if (!existsSync(this.configuration.luisFolder)) {
            throw new Error(`Path to the LUIS folder (${ this.configuration.luisFolder }) leads to a nonexistent folder.${
                EOL }Remember to use the argument '--luisFolder' for your Skill's LUIS folder.`);
        }

        let luFile = '';
        let luisFile = '';
        let luFilePath = '';
        let luisFolderPath: string = join(this.configuration.luisFolder, culture);
        let luisFilePath = '';
        let dispatchFolderPath = '';
        let dispatchFilePath = '';
        let useAllIntents = false;

        dispatchFolderPath = join(this.configuration.dispatchFolder, culture);
        dispatchFilePath = join(dispatchFolderPath, `${ dispatchName }.dispatch`);

        // Validate 'dispatch add' arguments
        if (!existsSync(dispatchFolderPath)) {
            throw new Error(
                `Path to the Dispatch folder (${ dispatchFolderPath }) leads to a nonexistent folder.${
                    EOL }Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        } else if (!existsSync(dispatchFilePath)) {
            throw new Error(`Path to the ${ dispatchName }.dispatch file leads to a nonexistent file.`);
        }

        luFile = `${ luisApp }.lu`;
        luisFile = `${ luisApp }.luis`;
        luFilePath = join(this.configuration.luisFolder, culture, luFile);
        luisFilePath = join(luisFolderPath, luisFile);

        if (!existsSync(luFilePath)) {
            if (this.manifest !== undefined && this.manifest.entries !== undefined) {

                const model: IModel = {id: '', name: '', contentType: '', url: '', description: ''};
                const currentLocaleApps = this.manifest.entries.find((entry: [string, IModel[]]): boolean => entry[0] === culture) || [model];
                const localeApps: IModel[] = currentLocaleApps[1];
                const currentApp: IModel = localeApps.find((model: IModel): boolean => model.id === luisApp) || model;

                if (currentApp.url.startsWith('file')) {
                    luFilePath = currentApp.url.split('file://')[1];
                    if(!existsSync(luFilePath)) {
                        luFile = luFilePath;
                        luisFile = `${ parse(luFilePath).name }.luis`;
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

                if (luFile.trim().length === 0) {
                    luisFile = `${ parse(luFilePath).name }.luis`;
                }
                luisFilePath = join(luisFolderPath, luisFile);
            }
        }

        // Validate 'bf luis:convert' arguments
        if (!existsSync(luFilePath)) {
            throw new Error(`Path to the ${ luFile } file leads to a nonexistent file.${
                EOL }Make sure your Skill's .lu file's name matches your Skill's manifest id`);
        }

        const executionModelMap: Map<string, string> = new Map();
        executionModelMap.set('luisApp', luisApp);
        executionModelMap.set('luisFile', luisFile);
        executionModelMap.set('luisFilePath', luisFilePath);
        executionModelMap.set('--in', luFilePath);
        executionModelMap.set('--culture', culture);
        executionModelMap.set('--out', luisFilePath);
        executionModelMap.set('--type', 'file');
        executionModelMap.set('--name', intentName);
        executionModelMap.set('--filePath', luisFilePath);
        executionModelMap.set('--intentName', intentName);
        executionModelMap.set('--dataFolder', dispatchFolderPath);
        executionModelMap.set('--dispatch', dispatchFilePath);

        // Validation of filtered intents
        if (this.manifest !== undefined) {
            useAllIntents = this.manifest.allowedIntents.some(e => e === '*');

            if (useAllIntents && this.manifest.allowedIntents.length > 1) {
                this.logger.warning('Found intent with name \'*\'. Adding all intents.');
            }
            
            if (!useAllIntents && this.manifest.allowedIntents.length > 0) {
                executionModelMap.set('--includedIntents', this.manifest.allowedIntents.join(','));
            }
        }
        
        return executionModelMap;
    }

    private async runCommand(command: string[], description: string): Promise<string> {
        this.logger.command(description, command.join(' '));
        const cmd: string = command[0];
        const commandArgs: string[] = command.slice(1)
            .filter((arg: string): string => arg);

        try {
            return await this.childProcessUtils.execute(cmd, commandArgs);
        } catch (err) {
            throw new Error(`The execution of the ${ cmd } command failed with the following error:${ EOL + err }`);
        }
    }

    private async getRemoteLu(path: string): Promise<string> {
        try {
            return get({
                uri: path
            });
        } catch (err) {
            throw new Error(`There was a problem while getting the remote lu file:${ EOL + err }`);
        }
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
            throw new Error(`Some of the cultures provided to connect from the Skill are not available or aren't supported by your VA.${
                EOL }Make sure you have a Dispatch for the cultures you are trying to connect, and that your Skill has a LUIS model for that culture`);
        }
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
                luisConvertCommand.push(...[argument, wrapPathWithQuotes(argumentValue)]);
            });
            luisConvertCommand.push('--force');
            await this.runCommand(luisConvertCommand, `Parsing ${ culture } ${ luisApp } LU file`);
            if (!existsSync(luisFilePath)) {
                throw new Error(`Path to ${ luisFile } (${ luisFilePath }) leads to a nonexistent file.`);
            }
        } catch (err) {
            throw new Error(`There was an error in the bf luis:convert command:${ EOL }Command: ${ luisConvertCommand.join(' ') + EOL + err }`);
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
            throw new Error(`There was an error in the dispatch add command:${ EOL }Command: ${ dispatchAddCommand.join(' ') + EOL + err }`);
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
                    if (!this.logger.isError) {
                        await this.executeDispatchAdd(culture, executionModelByCulture);
                    }
                }));

            if (this.logger.isError) {
                throw new Error('There were one or more issues converting the LU files. Aborting the process.');
            }

            // Check if it is necessary to refresh the skill
            if (!this.configuration.noRefresh) {
                await this.executeRefresh();
            } else {
                this.logger.warning(`Run 'botskills refresh --${ this.configuration.lgLanguage }' command to refresh your connected skills`);
            }
        } catch (err) {
            throw new Error(`An error ocurred while updating the Dispatch model:${ EOL + err }`);
        }
    }

    public async connectSkill(): Promise<boolean> {
        try {
            // Validate if the user has the necessary tools to run the command
            await validateLibrary([libraries.BotFrameworkCLI], this.logger);
            if (this.logger.isError) {
                throw new Error('You have not installed the required tools to run this command');
            }

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
            const rawManifest: string = await this.manifestUtils.getRawManifestFromResource(this.configuration);
            this.manifest = await this.manifestUtils.getManifest(rawManifest, this.logger);
            await this.connectSkillManifest(cognitiveModelsFile, this.manifest);

            return true;
           
        } catch (err) {
            this.logger.error(`There was an error while connecting the Skill to the Assistant:${ EOL + err }`);
            return false;
        }
    }

    private async AddSkill(assistantSkillsFile: IAppSetting, assistantSkills: ISkill[], skill: IManifest): Promise<void> {
        assistantSkills.push({
            id: skill.id,
            appId: skill.msaAppId,
            skillEndpoint: skill.endpoint,
            name: skill.name,
            description: skill.description
        });

        assistantSkillsFile.botFrameworkSkills = assistantSkills;
         
        if (assistantSkillsFile.skillHostEndpoint === undefined || assistantSkillsFile.skillHostEndpoint.trim().length === 0) {
            const channel: string = await isCloudGovernment() ? 'us' : 'net';
            assistantSkillsFile.skillHostEndpoint = `https://${ this.configuration.botName }.azurewebsites.${ channel }/api/skills`;
        }
        writeFileSync(this.configuration.appSettingsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
    }

    private async connectSkillManifest(cognitiveModelsFile: ICognitiveModel, skillManifest: IManifest): Promise<void> {
        try {
            // Take VA Skills configurations
            const assistantSkillsFile: IAppSetting = JSON.parse(sanitizeAppSettingsProperties(this.configuration.appSettingsFile));
            const assistantSkills: ISkill[] = assistantSkillsFile.botFrameworkSkills !== undefined ? assistantSkillsFile.botFrameworkSkills : [];

            // Check if the skill is already connected to the assistant
            if (assistantSkills.find((assistantSkill: ISkill): boolean => assistantSkill.id === skillManifest.id)) {
                this.logger.warning(`The skill with ID '${ skillManifest.id }' is already registered.`);
                return;
            }

            // Validate cultures
            await this.validateCultures(cognitiveModelsFile, skillManifest.luisDictionary);
            // Updating Dispatch
            this.logger.message('Updating Dispatch');
            await this.updateModel(skillManifest.luisDictionary, skillManifest.id);
            // Adding the skill manifest to the assistant skills array
            this.logger.message(`Appending '${ skillManifest.name }' manifest to your assistant's skills configuration file.`);
            // Updating the assistant skills file's skills property with the assistant skills array
            // Writing (and overriding) the assistant skills file
            //writeFileSync(this.configuration.skillsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
            await this.AddSkill(assistantSkillsFile, assistantSkills, skillManifest);
            this.logger.success(`Successfully appended '${ skillManifest.name }' manifest to your assistant's skills configuration file!`);
            // Configuring bot auth settings
            //this.logger.message('Configuring bot auth settings');
            //await this.authenticationUtils.authenticate(this.configuration, skillManifest, this.logger);
        } catch (err) {
            this.logger.error(`There was an error while connecting the Skill to the Assistant:${ EOL + err }`);
        }
    }
}
