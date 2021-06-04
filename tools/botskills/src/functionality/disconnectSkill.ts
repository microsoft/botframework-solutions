/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync, writeFileSync } from 'fs';
import { join } from 'path';
import { RefreshSkill } from '../functionality';
import { ConsoleLogger, ILogger } from '../logger';
import {
    ICognitiveModel,
    IDisconnectConfiguration,
    IDispatchFile,
    IDispatchService,
    IRefreshConfiguration,
    IAppSetting,
    ISkill
} from '../models';
import { getDispatchNames, sanitizeAppSettingsProperties } from '../utils';
import { EOL } from 'os';

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
                this.configuration.cognitiveModelsFile }). Please provide the '--cognitiveModelsFile' argument.`);
        }
        const cognitiveModelsFile: ICognitiveModel = JSON.parse(readFileSync(this.configuration.cognitiveModelsFile, 'UTF8'));
        const dispatchNames: Map<string, string> = getDispatchNames(cognitiveModelsFile);
        Array.from(dispatchNames.entries())
            .map((item: [string, string]): void => {
                const culture: string = item[0];
                const dispatchName: string = item[1];
                const dispatchFilePath: string = join(this.configuration.dispatchFolder, culture, `${ dispatchName }.dispatch`);
                if (existsSync(dispatchFilePath)) {
                    const dispatchData: IDispatchFile = JSON.parse(
                        readFileSync(dispatchFilePath)
                            .toString());
                    const serviceToRemove: IDispatchService | undefined = dispatchData.services.find((service: IDispatchService): boolean =>
                        service.name === this.configuration.skillId);
                    if (serviceToRemove) {
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
                    throw new Error(`The path to the dispatch file doesn't exists: ${ dispatchFilePath }`);
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
                this.logger.warning(`Run 'botskills refresh --${ this.configuration.lgLanguage }' command to refresh your connected skills`);
            }
        } catch (err) {
            throw new Error(`An error ocurred while updating the Dispatch model:${ EOL + err }`);
        }
    }

    public async disconnectSkill(): Promise<boolean> {
        try {
            // Validate configuration.appSettingsFile
            if (!existsSync(this.configuration.appSettingsFile)) {
                this.logger.error(`The 'appSettingsFile' argument is absent or leads to a non-existing file.${
                    EOL }Please make sure to provide a valid path to your Assistant Skills configuration file using the '--appSettingsFile' argument.`);

                return false;
            }

            // Take VA Skills configurations
            const assistantSkillsFile: IAppSetting = JSON.parse(sanitizeAppSettingsProperties(this.configuration.appSettingsFile));
            const assistantSkills: ISkill[] = assistantSkillsFile.botFrameworkSkills !== undefined ? assistantSkillsFile.botFrameworkSkills : [];

            // Check if the skill is present in the assistant
            const skillToRemove: ISkill | undefined = assistantSkills.find((assistantSkill: ISkill): boolean =>
                assistantSkill.id === this.configuration.skillId
            );

            if (!skillToRemove) {
                this.logger.warning(`The skill '${ this.configuration.skillId }' is not present in the assistant Skills configuration file.${
                    EOL }Run 'botskills list --appSettingsFile "<YOUR-APPSETTINGS-FILE-PATH>"' in order to list all the skills connected to your assistant`);

                return false;
            } else if (!this.configuration.lgLanguage || !(['cs', 'ts'].includes(this.configuration.lgLanguage))) {
                this.logger.error(`The 'lgLanguage' argument is incorrect.${
                    EOL }It should be either 'cs' or 'ts' depending on your assistant's language. Please provide either the argument '--cs' or '--ts'.`);

                return false;
            } else if (!this.configuration.lgOutFolder || !existsSync(this.configuration.lgOutFolder)) {
                this.logger.error(`The 'lgOutFolder' argument is absent or leads to a non-existing folder.${
                    EOL }Please make sure to provide a valid path to your Luis Generate output folder using the '--lgOutFolder' argument.`);

                return false;
            } else {
                await this.updateDispatch();

                // Removing the skill manifest from the assistant skills array
                this.logger.message(`Removing the '${ this.configuration.skillId }' skill from your assistant's skills configuration file.`);
                assistantSkills.splice(assistantSkills.indexOf(skillToRemove), 1);

                // Updating the assistant skills file's skills property with the assistant skills array
                assistantSkillsFile.botFrameworkSkills = assistantSkills;

                // Writing (and overriding) the assistant skills file
                writeFileSync(this.configuration.appSettingsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
                this.logger.success(
                    `Successfully removed '${ this.configuration.skillId }' skill from your assistant's skills configuration file.`);

                return true;
            }
        } catch (err) {
            this.logger.error(`There was an error while disconnecting the Skill ${ this.configuration.skillId } from the Assistant:${ EOL + err }`);

            return false;
        }
    }
}
