/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync, writeFileSync } from 'fs';
import { ConsoleLogger, ILogger} from '../logger';
import { IMigrateConfiguration, IAppSetting, ISkill, ISkillFileV1 } from '../models';
import { isInstanceOfISkillManifestV1 } from '../utils/validationUtils';

export class MigrateSkill {
    public logger: ILogger;
    public constructor(logger?: ILogger) {
        this.logger = logger || new ConsoleLogger();
    }
    public async migrateSkill(configuration: IMigrateConfiguration): Promise<boolean> {
        try {
            // Validate configuration.destFile
            if (!existsSync(configuration.destFile)) {
                this.logger.error(`The 'destFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--destFile' argument.`);

                return false;
            }

            // Validate configuration.sourceFile
            if (!existsSync(configuration.sourceFile)) {
                this.logger.error(`The 'sourceFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--sourceFile' argument.`);

                return false;
            }

            // Take VA Skills configurations
            const sourceAssistantSkills: ISkillFileV1 = JSON.parse(readFileSync(configuration.sourceFile, 'UTF8'));
            if (sourceAssistantSkills.skills === undefined || sourceAssistantSkills.skills.length === 0) {
                this.logger.message('There are no Skills in the source file.');

                return false;
            }

            const destFile: IAppSetting = JSON.parse(readFileSync(configuration.destFile, 'UTF8'));

            const destAssistantSkills: ISkill[] = destFile.BotFrameworkSkills || [];

            sourceAssistantSkills.skills.forEach((skill): void => {
                if (isInstanceOfISkillManifestV1(skill)){

                    if (destAssistantSkills.find((assistantSkill: ISkill): boolean => assistantSkill.Id === skill.id)) {
                        this.logger.warning(`The skill '${ skill.name }' is already registered.`);
                        return;
                    }

                    destAssistantSkills.push({
                        Id: skill.id,
                        AppId: skill.msaAppId,
                        SkillEndpoint: skill.endpoint,
                        Name: skill.name
                    });
                }
                else {
                    throw new Error(`A skill has an incorrect format, please check that all the skills intended to be migrated has the V1 format`);
                }
            });

            destFile.BotFrameworkSkills = destAssistantSkills;
            writeFileSync(configuration.destFile, JSON.stringify(destFile, undefined, 4));

            this.logger.success(`Successfully migrated all the skills for the new version`);
            return true;
        } catch (err) {
            this.logger.error(`There was an error while migrating the Skills:\n ${ err }`);

            return false;
        }
    }
}
