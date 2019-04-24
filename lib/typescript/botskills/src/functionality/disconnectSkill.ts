/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { writeFileSync } from 'fs';
import { ConsoleLogger, ILogger } from '../logger';
import { IDisconnectConfiguration, ISkillManifest } from '../models/';

export async function disconnectSkill(configuration: IDisconnectConfiguration): Promise<void> {
    if (configuration.logger) {
        logger = configuration.logger;
    }

    // Take VA Skills configurations
    //tslint:disable-next-line: no-var-requires non-literal-require
    const assistantSkills: ISkillManifest[] = require(configuration.skillsFile).skills;
    // Check if the skill is present in the assistant
    const skillToRemove: ISkillManifest | undefined = assistantSkills.find((assistantSkill: ISkillManifest) =>
        (assistantSkill.id === configuration.skillName) || (assistantSkill.name === configuration.skillName)
    );

    if (!skillToRemove) {
        logger.warning(`The skill '${configuration.skillName}' is not present in the assistant Skills configuration file.
Run 'botskills list --assistantSkills "<YOUR-ASSISTANT-SKILLS-FILE-PATH>"' in order to list all the skills connected to your assistant`);
        process.exit(1);
    } else {
        // Removing the skill manifest from the assistant skills array
        logger.warning(`Removing the '${configuration.skillName}' skill from your assistant's skills configuration file.`);
        assistantSkills.splice(assistantSkills.indexOf(skillToRemove), 1);

        // Writing (and overriding) the assistant skills file
        writeFileSync(configuration.skillsFile, JSON.stringify(assistantSkills, undefined, 4));
        logger.success(`Successfully removed '${configuration.skillName}' skill from your assistant's skills configuration file.`);
    }
}

let logger: ILogger = new ConsoleLogger();
