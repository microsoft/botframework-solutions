/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync, writeFileSync } from 'fs';
import { join } from 'path';
import { ConsoleLogger, ILogger } from '../logger';
import { IDisconnectConfiguration, IDispatchFile, IDispatchService, ISkillFIle, ISkillManifest } from '../models/';
import { ChildProcessUtils } from '../utils';

export class DisconnectSkill {
    public logger: ILogger;
    private childProcessUtils: ChildProcessUtils;
    constructor(logger?: ILogger) {
        this.logger = logger || new ConsoleLogger();
        this.childProcessUtils = new ChildProcessUtils();
    }
    private async runCommand(command: string[], description: string): Promise<string> {
        this.logger.command(description, command.join(' '));
        const cmd: string = command[0];
        const commandArgs: string[] = command.slice(1)
            .filter((arg: string) => arg);

        try {
            return await this.childProcessUtils.execute(cmd, commandArgs);
        } catch (err) {

            throw err;
        }
    }

    public async updateDispatch(configuration: IDisconnectConfiguration): Promise<boolean> {
        try {
            // Initializing variables for the updateDispatch scope
            const dispatchFile: string = `${configuration.dispatchName}.dispatch`;
            const dispatchJsonFile: string = `${configuration.dispatchName}.json`;
            const dispatchFilePath: string = join(configuration.dispatchFolder, dispatchFile);
            const dispatchJsonFilePath: string = join(configuration.dispatchFolder, dispatchJsonFile);

            this.logger.message('Removing skill from dispatch...');

            // dispatch remove(?)
            if (!existsSync(dispatchFilePath)) {
                this.logger.error(
                    `Could not find file ${dispatchFile}. Please provide the 'dispatchName' and 'dispatchFolder' parameters.`);

                return false;
            }
            // tslint:disable-next-line:no-var-require non-literal-require
            const dispatchData: IDispatchFile = JSON.parse(
                readFileSync(dispatchFilePath)
                .toString());
            const serviceToRemove: IDispatchService | undefined = dispatchData.services.find((service: IDispatchService) =>
                service.name === configuration.skillId);
            if (!serviceToRemove) {
                this.logger.warning(`The skill ${configuration.skillId} is not present in the Dispatch model.
Run 'botskills list --assistantSkills "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);

                return false;
            }
            dispatchData.serviceIds.splice(dispatchData.serviceIds.findIndex((serviceId: string) => serviceId === serviceToRemove.id), 1);
            const skillIndex: number = dispatchData.services.findIndex(
                (service: IDispatchService) => service.name === configuration.skillId);
            dispatchData.services.splice(
                skillIndex,
                1);
            writeFileSync(dispatchFilePath, JSON.stringify(dispatchData, undefined, 4));

            // Check if it is necessary to train the skill
            if (!configuration.noTrain) {
                this.logger.message('Running Dispatch refresh');
                const dispatchRefreshCommand: string[] = ['dispatch', 'refresh'];
                dispatchRefreshCommand.push(...['--dispatch', dispatchFilePath]);
                dispatchRefreshCommand.push(...['--dataFolder', configuration.dispatchFolder]);
                await this.runCommand(dispatchRefreshCommand, `Executing dispatch refresh for the ${configuration.dispatchName} file`);

                if (!existsSync(dispatchJsonFilePath)) {
                    // this.logger.error(`Path to ${dispatchJsonFile} (${dispatchJsonFilePath}) leads
                    // to a nonexistent file. Make sure the dispatch refresh command is being executed successfully`);
                    // tslint:disable-next-line: max-line-length
                    throw(new Error(`Path to ${dispatchJsonFile} (${dispatchJsonFilePath}) leads to a nonexistent file. Make sure the dispatch refresh command is being executed successfully`));
                }

                this.logger.message('Running LuisGen...');

                const luisgenCommand: string[] = ['luisgen'];
                luisgenCommand.push(dispatchJsonFilePath);
                luisgenCommand.push(...[`-${configuration.lgLanguage}`, '"DispatchLuis"']);
                luisgenCommand.push(...['-o', configuration.lgOutFolder]);
                await this.runCommand(luisgenCommand, `Executing luisgen for the ${configuration.dispatchName} file`);
            }

            return true;
        } catch (err) {
            throw new Error(`An error ocurred while updating the Dispatch model:\n${err}`);
        }
    }

    public async disconnectSkill(configuration: IDisconnectConfiguration): Promise<boolean> {
        try {
            // Validate configuration.skillsFile
            if (!existsSync(configuration.skillsFile)) {
                this.logger.error(`The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file.`);

                return false;
            }

            // Take VA Skills configurations
            //tslint:disable-next-line: no-var-requires non-literal-require
            const assistantSkillsFile: ISkillFIle = require(configuration.skillsFile);
            const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills || [];

            // Check if the skill is present in the assistant
            const skillToRemove: ISkillManifest | undefined = assistantSkills.find((assistantSkill: ISkillManifest) =>
                assistantSkill.id === configuration.skillId
            );

            if (!skillToRemove) {
                this.logger.warning(`The skill '${configuration.skillId}' is not present in the assistant Skills configuration file.
Run 'botskills list --assistantSkills "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);

                return false;
            } else if (!configuration.lgLanguage || !(['cs', 'ts'].includes(configuration.lgLanguage))) {
                this.logger.error(`The 'lgLanguage' argument is incorrect.
It should be either 'cs' or 'ts' depending on your assistant's language.`);

                return false;
            } else if (!configuration.lgOutFolder || !existsSync(configuration.lgOutFolder)) {
                this.logger.error(`The 'lgOutFolder' argument is absent or leads to a non-existing folder.
Please make sure to provide a valid path to your LUISGen output folder.`);

                return false;
            } else {
                if (!(await this.updateDispatch(configuration))) {

                    return false;
                }

                // Removing the skill manifest from the assistant skills array
                this.logger.warning(`Removing the '${configuration.skillId}' skill from your assistant's skills configuration file.`);
                assistantSkills.splice(assistantSkills.indexOf(skillToRemove), 1);

                // Updating the assistant skills file's skills property with the assistant skills array
                assistantSkillsFile.skills = assistantSkills;

                // Writing (and overriding) the assistant skills file
                writeFileSync(configuration.skillsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
                this.logger.success(
                    `Successfully removed '${configuration.skillId}' skill from your assistant's skills configuration file.`);

                return true;
            }
        } catch (err) {
            this.logger.error(`There was an error while disconnecting the Skill ${configuration.skillId} from the Assistant:\n${err}`);

            return false;
        }
    }
}
