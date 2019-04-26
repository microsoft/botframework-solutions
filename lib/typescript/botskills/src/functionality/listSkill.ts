/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ConsoleLogger, ILogger} from '../logger';
import { IListConfiguration, ISkillFIle, ISkillManifest } from '../models';

export async function listSkill(configuration: IListConfiguration): Promise<void> {
    if (configuration.logger) {
        logger = configuration.logger;
    }

    // Take VA Skills configurations
    //tslint:disable-next-line:non-literal-require
    const assistantSkillsFile: ISkillFIle = require(configuration.skillsFile);
    const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills;

    let message: string = `The skills already connected to the assistant are the following:`;
    assistantSkills.forEach((skillManifest: ISkillManifest) => {
        message += `\n\t- ${skillManifest.name}`;
    });

    logger.message(message);
}

let logger: ILogger = new ConsoleLogger();
