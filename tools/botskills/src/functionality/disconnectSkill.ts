/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync, writeFileSync } from 'fs';
import { join } from 'path';
import { RefreshSkill } from '../functionality';
import { ConsoleLogger, ILogger } from '../logger';
import { IDisconnectConfiguration, IDispatchFile, IDispatchService, IRefreshConfiguration, ISkillFile, ISkillManifest } from '../models';

export class DisconnectSkill {
    public logger: ILogger;
    private refreshSkill: RefreshSkill;

    constructor(logger?: ILogger) {
        this.logger = logger || new ConsoleLogger();
        this.refreshSkill = new RefreshSkill(this.logger);
    }

    public async updateDispatch(configuration: IDisconnectConfiguration): Promise<boolean> {
        try {
            // Initializing variables for the updateDispatch scope
            const dispatchFile: string = `${configuration.dispatchName}.dispatch`;
            const dispatchFilePath: string = join(configuration.dispatchFolder, dispatchFile);
            this.logger.message('Removing skill from dispatch...');

            // dispatch remove(?)
            if (!existsSync(dispatchFilePath)) {
                this.logger.error(
                    `Could not find file ${dispatchFile}. Please provide the '--dispatchName' and '--dispatchFolder' arguments.`);

                return false;
            }
            const dispatchData: IDispatchFile = JSON.parse(
                readFileSync(dispatchFilePath)
                .toString());
            const serviceToRemove: IDispatchService | undefined = dispatchData.services.find((service: IDispatchService) =>
                service.name === configuration.skillId);
            if (!serviceToRemove) {
                this.logger.warning(`The skill ${configuration.skillId} is not present in the Dispatch model.
Run 'botskills list --skillsFile "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);

                return false;
            }
            dispatchData.serviceIds.splice(dispatchData.serviceIds.findIndex((serviceId: string) => serviceId === serviceToRemove.id), 1);
            const skillIndex: number = dispatchData.services.findIndex(
                (service: IDispatchService) => service.name === configuration.skillId);
            dispatchData.services.splice(
                skillIndex,
                1);
            writeFileSync(dispatchFilePath, JSON.stringify(dispatchData, undefined, 4));

            // Check if it is necessary to refresh the skill
            if (!configuration.noRefresh) {
                const refreshConfiguration: IRefreshConfiguration = {...{}, ...configuration};
                if (!await this.refreshSkill.refreshSkill(refreshConfiguration)) {
                    throw new Error(`There was an error while refreshing the Dispatch model.`);
                }
            } else {
                this.logger.warning(`Run 'botskills refresh --${configuration.lgLanguage}' command to refresh your connected skills`);
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
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--skillsFile' argument.`);

                return false;
            }

            // Take VA Skills configurations
            const assistantSkillsFile: ISkillFile = JSON.parse(readFileSync(configuration.skillsFile, 'UTF8'));
            const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills || [];

            // Check if the skill is present in the assistant
            const skillToRemove: ISkillManifest | undefined = assistantSkills.find((assistantSkill: ISkillManifest) =>
                assistantSkill.id === configuration.skillId
            );

            if (!skillToRemove) {
                this.logger.warning(`The skill '${configuration.skillId}' is not present in the assistant Skills configuration file.
Run 'botskills list --skillsFile "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);

                return false;
            } else if (!configuration.lgLanguage || !(['cs', 'ts'].includes(configuration.lgLanguage))) {
                this.logger.error(`The 'lgLanguage' argument is incorrect.
It should be either 'cs' or 'ts' depending on your assistant's language. Please provide either the argument '--cs' or '--ts'.`);

                return false;
            } else if (!configuration.lgOutFolder || !existsSync(configuration.lgOutFolder)) {
                this.logger.error(`The 'lgOutFolder' argument is absent or leads to a non-existing folder.
Please make sure to provide a valid path to your LUISGen output folder using the '--lgOutFolder' argument.`);

                return false;
            } else {
                if (!(await this.updateDispatch(configuration))) {

                    return false;
                }

                // Removing the skill manifest from the assistant skills array
                this.logger.message(`Removing the '${configuration.skillId}' skill from your assistant's skills configuration file.`);
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
