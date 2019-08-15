/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync, writeFileSync } from 'fs';
import { join } from 'path';
import { RefreshSkill } from '../functionality';
import { ConsoleLogger, ILogger } from '../logger';
import {
    ICognitiveModelFile,
    IDisconnectConfiguration,
    IDispatchFile,
    IDispatchService,
    IRefreshConfiguration,
    ISkillFile,
    ISkillManifest
} from '../models';
import { getDispatchNames } from '../utils';

export class DisconnectSkill {
    private readonly configuration: IDisconnectConfiguration;
    private readonly logger: ILogger;

    public constructor(configuration: IDisconnectConfiguration, logger?: ILogger) {
        this.configuration = configuration;
        this.logger = logger || new ConsoleLogger();
    }

    private removeSkill(): void {
        if (!existsSync(this.configuration.cognitiveModelsFile)) {
            throw new Error(`Could not find the cognitiveModels file (${
                this.configuration.cognitiveModelsFile}). Please provide the '--cognitiveModelsFile' argument.`);
        }
        // eslint-disable-next-line @typescript-eslint/tslint/config
        const cognitiveModelsFile: ICognitiveModelFile = JSON.parse(readFileSync(this.configuration.cognitiveModelsFile, 'UTF8'));
        const dispatchNames: Map<string, string> = getDispatchNames(cognitiveModelsFile);
        Array.from(dispatchNames.entries())
            .map((item: [string, string]): void => {
                const culture: string = item[0];
                const dispatchName: string = item[1];
                const dispatchFilePath: string = join(this.configuration.dispatchFolder, culture, `${dispatchName}.dispatch`);
                if (existsSync(dispatchFilePath)) {
                    // eslint-disable-next-line @typescript-eslint/tslint/config
                    const dispatchData: IDispatchFile = JSON.parse(
                        readFileSync(dispatchFilePath)
                            .toString());
                    const serviceToRemove: IDispatchService | undefined = dispatchData.services.find((service: IDispatchService): boolean =>
                        service.name === this.configuration.skillId);
                    if (!serviceToRemove) {
                        this.logger.warning(`The skill ${this.configuration.skillId} is not present in the Dispatch model.
Run 'botskills list --skillsFile "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);
                    } else {
                        dispatchData.serviceIds.splice(
                            dispatchData.serviceIds.findIndex(
                                (serviceId: string): boolean => serviceId === serviceToRemove.id),
                            1);
                        const skillIndex: number = dispatchData.services.findIndex(
                            (service: IDispatchService): boolean => service.name === this.configuration.skillId);
                        dispatchData.services.splice(
                            skillIndex,
                            1);
                        writeFileSync(dispatchFilePath, JSON.stringify(dispatchData, undefined, 4));
                    }
                } else {
                    throw new Error(`The path to the dispatch file doesn't exists: ${dispatchFilePath}`);
                }
            });
    }

    private async executeRefresh(): Promise<void> {
        const refreshConfiguration: IRefreshConfiguration = { ...{}, ...this.configuration };
        const refreshSkill: RefreshSkill = new RefreshSkill(refreshConfiguration, this.logger);
        if (!await refreshSkill.refreshSkill()) {
            throw new Error(`There was an error while refreshing the Dispatch model.`);
        }
    }

    private async updateDispatch(): Promise<void> {
        try {
            this.removeSkill();

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

    public async disconnectSkill(): Promise<boolean> {
        try {
            // Validate configuration.skillsFile
            if (!existsSync(this.configuration.skillsFile)) {
                this.logger.error(`The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--skillsFile' argument.`);

                return false;
            }

            // Take VA Skills configurations
            // eslint-disable-next-line @typescript-eslint/tslint/config
            const assistantSkillsFile: ISkillFile = JSON.parse(readFileSync(this.configuration.skillsFile, 'UTF8'));
            const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills !== undefined ? assistantSkillsFile.skills : [];

            // Check if the skill is present in the assistant
            const skillToRemove: ISkillManifest | undefined = assistantSkills.find((assistantSkill: ISkillManifest): boolean =>
                assistantSkill.id === this.configuration.skillId
            );

            if (!skillToRemove) {
                this.logger.warning(`The skill '${this.configuration.skillId}' is not present in the assistant Skills configuration file.
Run 'botskills list --skillsFile "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);

                return false;
            } else if (!this.configuration.lgLanguage || !(['cs', 'ts'].includes(this.configuration.lgLanguage))) {
                this.logger.error(`The 'lgLanguage' argument is incorrect.
It should be either 'cs' or 'ts' depending on your assistant's language. Please provide either the argument '--cs' or '--ts'.`);

                return false;
            } else if (!this.configuration.lgOutFolder || !existsSync(this.configuration.lgOutFolder)) {
                this.logger.error(`The 'lgOutFolder' argument is absent or leads to a non-existing folder.
Please make sure to provide a valid path to your LUISGen output folder using the '--lgOutFolder' argument.`);

                return false;
            } else {
                await this.updateDispatch();

                // Removing the skill manifest from the assistant skills array
                this.logger.message(`Removing the '${this.configuration.skillId}' skill from your assistant's skills configuration file.`);
                assistantSkills.splice(assistantSkills.indexOf(skillToRemove), 1);

                // Updating the assistant skills file's skills property with the assistant skills array
                assistantSkillsFile.skills = assistantSkills;

                // Writing (and overriding) the assistant skills file
                writeFileSync(this.configuration.skillsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
                this.logger.success(
                    `Successfully removed '${this.configuration.skillId}' skill from your assistant's skills configuration file.`);

                return true;
            }
        } catch (err) {
            this.logger.error(`There was an error while disconnecting the Skill ${this.configuration.skillId} from the Assistant:\n${err}`);

            return false;
        }
    }
}
