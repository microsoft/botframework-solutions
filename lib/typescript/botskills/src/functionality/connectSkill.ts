/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync, writeFileSync } from 'fs';
import { isAbsolute, join, resolve } from 'path';
import { get } from 'request-promise-native';
import { ConsoleLogger, ILogger} from '../logger';
import { IAction, IConnectConfiguration, ISkillFIle, ISkillManifest, IUtteranceSource } from '../models';
import { AuthenticationUtils, ChildProcessUtils } from '../utils';

export class ConnectSkill {
    private logger: ILogger;
    private childProcessUtils: ChildProcessUtils;
    private authenticationUtils: AuthenticationUtils;

    constructor(logger: ILogger) {
        this.logger = logger || new ConsoleLogger();
        this.childProcessUtils = new ChildProcessUtils();
        this.authenticationUtils = new AuthenticationUtils();
    }

    public async runCommand(command: string[], description: string): Promise<string> {
        this.logger.command(description, command.join(' '));
        const cmd: string = command[0];
        const commandArgs: string[] = command.slice(1)
            .filter((arg: string) => arg);

        try {

            return await this.childProcessUtils.execute(cmd, commandArgs)
            // tslint:disable-next-line:typedef
            .catch((err) => {
                throw new Error(`The execution of the ${cmd} command failed with the following error:\n${err}`);
            });
        } catch (err) {
            throw err;
        }
    }

    public async getRemoteManifest(manifestUrl: string): Promise<ISkillManifest> {
        try {
            return get({
                uri: <string> manifestUrl,
                json: true
            });
        } catch (err) {
            throw new Error(`There was a problem while getting the remote manifest:\n${err}`);
        }
    }

    private getLocalManifest(manifestPath: string): ISkillManifest {
        const skillManifestPath: string = isAbsolute(manifestPath) ? manifestPath : join(resolve('./'), manifestPath);

        if (!existsSync(skillManifestPath)) {
            throw new Error(
            `The 'localManifest' argument leads to a non-existing file. Please make sure to provide a valid path to your Skill manifest.`);
        }

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
        if (!skillManifest.authenticationConnections) {
            this.logger.error(`Missing property 'authenticationConnections' of the manifest`);
        }
        if (!skillManifest.actions || !skillManifest.actions[0]) {
            this.logger.error(`Missing property 'actions' of the manifest`);
        }
    }

    // tslint:disable-next-line:max-func-body-length
    public async updateDispatch(configuration: IConnectConfiguration, manifest: ISkillManifest): Promise<void> {
        try {
            // Initializing variables for the updateDispatch scope
            const dispatchFile: string = `${configuration.dispatchName}.dispatch`;
            const dispatchJsonFile: string = `${configuration.dispatchName}.json`;
            const dispatchFilePath: string = join(configuration.dispatchFolder, dispatchFile);
            const dispatchJsonFilePath: string = join(configuration.dispatchFolder, dispatchJsonFile);
            const intentName: string = manifest.id;
            let luisDictionary: Map<string, string>;

            this.logger.message('Getting intents for dispatch...');
            luisDictionary = manifest.actions.filter((action: IAction) => action.definition.triggers.utteranceSources)
            .reduce((acc: IUtteranceSource[], val: IAction) => acc.concat(val.definition.triggers.utteranceSources), [])
            .reduce((acc: string[], val: IUtteranceSource) => acc.concat(val.source), [])
            .reduce(
                (acc: Map<string, string>, val: string) => {
                const luis: string[] = val.split('#');
                if (acc.has(luis[0])) {
                    const previous: string | undefined = acc.get(luis[0]);
                    acc.set(luis[0], previous + luis[1]);
                } else {
                    acc.set(luis[0], luis[1]);
                }

                return acc;
                },
                new Map());

            this.logger.message('Adding skill to Dispatch');
            await Promise.all(
            Array.from(luisDictionary.entries())
            .map(async(item: [string, string]) => {
                const luisApp: string = item[0];
                const luFile: string = `${luisApp}.lu`;
                const luisFile: string = `${luisApp}.luis`;
                const luFilePath: string = join(configuration.luisFolder, luFile);
                const luisFilePath: string = join(configuration.luisFolder, luisFile);

                // Validate 'ludown' arguments
                if (!existsSync(configuration.luisFolder)) {
                    throw(new Error(`Path to the LUIS folder (${configuration.luisFolder}) leads to a nonexistent folder.`));
                } else if (!existsSync(luFilePath)) {
                    throw(new Error(`Path to the ${luisApp}.lu file leads to a nonexistent file.`));
                }

                // Validate 'dispatch add' arguments
                if (!existsSync(configuration.dispatchFolder)) {
                    throw(new Error(`Path to the Dispatch folder (${configuration.dispatchFolder}) leads to a nonexistent folder.`));
                } else if (!existsSync(dispatchFilePath)) {
                    throw(new Error(`Path to the ${dispatchFile} file leads to a nonexistent file.`));
                }

                // Parse LU file
                this.logger.message(`Parsing ${luisApp} LU file...`);
                const ludownParseCommand: string[] = ['ludown', 'parse', 'toluis'];
                ludownParseCommand.push(...['--in', luFilePath]);
                ludownParseCommand.push(...['--luis_culture', configuration.language]);
                ludownParseCommand.push(...['--out_folder', configuration.luisFolder]);
                ludownParseCommand.push(...['--out', `"${luisFile}"`]);

                await this.runCommand(ludownParseCommand, `Parsing ${luisApp} LU file`);

                if (!existsSync(luisFilePath)) {
                    // tslint:disable-next-line: max-line-length
                    throw(new Error(`Path to ${luisFile} (${luisFilePath}) leads to a nonexistent file. Make sure the ludown command is being executed successfully`));
                }
                // Update Dispatch file
                const dispatchAddCommand: string[] = ['dispatch', 'add'];
                dispatchAddCommand.push(...['--type', 'file']);
                dispatchAddCommand.push(...['--name', intentName]);
                dispatchAddCommand.push(...['--filePath', luisFilePath]);
                dispatchAddCommand.push(...['--intentName', intentName]);
                dispatchAddCommand.push(...['--dataFolder', configuration.dispatchFolder]);
                dispatchAddCommand.push(...['--dispatch', dispatchFilePath]);

                await this.runCommand(dispatchAddCommand, `Executing dispatch add for the ${luisApp} LU file`);
            }));

            // Check if it is necessary to train the skill
            if (!configuration.noTrain) {
                this.logger.message('Running dispatch refresh...');
                const dispatchRefreshCommand: string[] = ['dispatch', 'refresh'];
                dispatchRefreshCommand.push(...['--dispatch', dispatchFilePath]);
                dispatchRefreshCommand.push(...['--dataFolder', configuration.dispatchFolder]);

                this.logger.message(await this.runCommand(
                    dispatchRefreshCommand,
                    `Executing dispatch refresh for the ${configuration.dispatchName} file`));

                if (!existsSync(dispatchJsonFilePath)) {
                    // tslint:disable-next-line: max-line-length
                    throw(new Error(`Path to ${dispatchJsonFile} (${dispatchJsonFilePath}) leads to a nonexistent file. Make sure the dispatch refresh command is being executed successfully`));
                }

                this.logger.message('Running LuisGen...');
                const luisgenCommand: string[] = ['luisgen'];
                luisgenCommand.push(dispatchJsonFilePath);
                luisgenCommand.push(...[`-${configuration.lgLanguage}`, `"DispatchLuis"`]);
                luisgenCommand.push(...['-o', configuration.lgOutFolder]);

                await this.runCommand(luisgenCommand, `Executing luisgen for the ${configuration.dispatchName} file`);
            }
            this.logger.success('Successfully updated Dispatch model');
        } catch (err) {
            throw new Error(`An error ocurred while updating the Dispatch model:\n${err}`);
        }
    }

    public async connectSkill(configuration: IConnectConfiguration): Promise<boolean> {
        try {
            // Validate if no manifest path or URL was passed
            if (!configuration.localManifest && !configuration.remoteManifest) {
                throw new Error(`Either the 'localManifest' or 'remoteManifest' argument should be passed.`);
            }
            // Take skillManifest
            const skillManifest: ISkillManifest = configuration.localManifest
            ? this.getLocalManifest(configuration.localManifest)
            : await this.getRemoteManifest(configuration.remoteManifest);

            // Manifest schema validation
            this.validateManifestSchema(skillManifest);

            if (this.logger.isError) {

                return false;
            }
            // End of manifest schema validation

            // Take VA Skills configurations
            //tslint:disable-next-line: no-var-requires non-literal-require
            const assistantSkillsFile: ISkillFIle = require(configuration.skillsFile);
            const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills || [];

            // Check if the skill is already connected to the assistant
            if (assistantSkills.find((assistantSkill: ISkillManifest) => assistantSkill.id === skillManifest.id)) {
                this.logger.warning(`The skill '${skillManifest.name}' is already registered.`);

                return false;
            }

            // Updating Dispatch
            this.logger.message('Updating Dispatch');
            await this.updateDispatch(configuration, skillManifest);

            // Adding the skill manifest to the assistant skills array
            this.logger.warning(`Appending '${skillManifest.name}' manifest to your assistant's skills configuration file.`);
            assistantSkills.push(skillManifest);

            // Updating the assistant skills file's skills property with the assistant skills array
            assistantSkillsFile.skills = assistantSkills;

            // Writing (and overriding) the assistant skills file
            writeFileSync(configuration.skillsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
            this.logger.success(`Successfully appended '${skillManifest.name}' manifest to your assistant's skills configuration file!`);

            // Configuring bot auth settings
            this.logger.message('Configuring bot auth settings');
            await this.authenticationUtils.authenticate(configuration, skillManifest, this.logger);

            return true;
        } catch (err) {
            this.logger.error(`There was an error while connecting the Skill to the Assistant:\n${err}`);

            return false;
        }
    }
}
