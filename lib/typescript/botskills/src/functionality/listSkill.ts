/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync } from 'fs';
import { ConsoleLogger, ILogger} from '../logger';
import { IListConfiguration, ISkillFIle, ISkillManifest } from '../models';

export async function listSkill(configuration: IListConfiguration): Promise<boolean> {
    const logger: ILogger = configuration.logger || new ConsoleLogger();
    try {
        // Validate configuration.skillsFile
        if (!existsSync(configuration.skillsFile)) {
            logger.error(`The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file.`);

            return false;
        }
        // Take VA Skills configurations
        //tslint:disable-next-line:non-literal-require
        const assistantSkillsFile: ISkillFIle = require(configuration.skillsFile);
        if (!assistantSkillsFile.skills) {
            logger.message('There are no Skills connected to the assistant.');

            return false;
        }
        const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills;

        if (assistantSkills.length < 1) {
            logger.message('There are no Skills connected to the assistant.');

            return false;
        } else {
            let message: string = `The skills already connected to the assistant are the following:`;
            assistantSkills.forEach((skillManifest: ISkillManifest) => {
                message += `\n\t- ${skillManifest.id}`;
            });

            logger.message(message);
        }

        return true;
    } catch (err) {
        logger.error(`There was an error while listing the Skills connected to your assistant:\n ${err}`);

        return false;
    }
}
