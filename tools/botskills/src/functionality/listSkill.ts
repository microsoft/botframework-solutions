/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, readFileSync } from 'fs';
import { ConsoleLogger, ILogger} from '../logger';
import { IListConfiguration, ISkillFile, ISkillManifest } from '../models';

export class ListSkill {
    public logger: ILogger;
    public constructor(logger?: ILogger) {
        this.logger = logger || new ConsoleLogger();
    }
    public async listSkill(configuration: IListConfiguration): Promise<boolean> {
        try {
            // Validate configuration.skillsFile
            if (!existsSync(configuration.skillsFile)) {
                this.logger.error(`The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--skillsFile' argument.`);

                return false;
            }
            // Take VA Skills configurations
            // eslint-disable-next-line @typescript-eslint/tslint/config
            const assistantSkillsFile: ISkillFile = JSON.parse(readFileSync(configuration.skillsFile, 'UTF8'));
            if (assistantSkillsFile.skills === undefined) {
                this.logger.message('There are no Skills connected to the assistant.');

                return false;
            }
            const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills;

            if (assistantSkills.length < 1) {
                this.logger.message('There are no Skills connected to the assistant.');

                return false;
            } else {
                let message: string = `The skills already connected to the assistant are the following:`;
                assistantSkills.forEach((skillManifest: ISkillManifest): void => {
                    message += `\n\t- ${skillManifest.id}`;
                });

                this.logger.message(message);
            }

            return true;
        } catch (err) {
            this.logger.error(`There was an error while listing the Skills connected to your assistant:\n ${err}`);

            return false;
        }
    }
}
