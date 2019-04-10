/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { existsSync, writeFileSync } from 'fs';
import { isAbsolute, join, resolve } from 'path';
import { ConsoleLogger, ILogger} from './logger/logger';
import { ISkillManifest } from './skillManifest';

function showErrorHelp(): void {
    program.outputHelp((str: string) => {
        logger.error(str);

        return '';
    });
    process.exit(1);
}

const logger: ILogger = new ConsoleLogger();

program.Command.prototype.unknownOption = (flag: string): void => {
    logger.error(`Unknown arguments: ${flag}`);
    showErrorHelp();
};

program
    .name('botskills disconnect')
    .description('Disconnect a specific skill from your assitant bot')
    .option('-s, --skillName <name>', 'Name of the skill to remove from your assistant')
    .option('-a, --assistantSkills <path>', 'Path to the assistant Skills configuration file')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
}

logger.isVerbose = args.verbose;

// Validation of arguments
// assistantSkills validation
if (!args.assistantSkills) {
    logger.error(`The 'assistantSkills' argument should be provided.`);
    process.exit(1);
} else if (args.assistantSkills.substring(args.assistantSkills.lastIndexOf('.') + 1) !== 'json') {
    logger.error(`The 'assistantSkills' argument should be a JSON file.`);
    process.exit(1);
}
const assistantSkillsPath: string = isAbsolute(args.assistantSkills) ? args.assistantSkills : join(resolve('./'), args.assistantSkills);
if (!existsSync(assistantSkillsPath)) {
    logger.error(
    `The 'assistantSkills' argument leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file.`);
    process.exit(1);
}

// skillName validation
if (!args.skillName) {
    logger.error(`The 'skillName' argument should be provided.`);
    process.exit(1);
}

// Take VA Skills configurations
//tslint:disable-next-line: no-var-requires non-literal-require
const assistantSkills: ISkillManifest[] = require(assistantSkillsPath);
// Check if the skill is present in the assistant
const skillToRemove: ISkillManifest | undefined = assistantSkills.find((assistantSkill: ISkillManifest) =>
    assistantSkill.name === args.skillName
);

if (!skillToRemove) {
    logger.warning(`The skill '${args.skillName}' is not present in the assistant Skills configuration file.
Run 'botskills list --assistantSkills "<YOUR-ASSISTANT-SKILLS-PATH>"' in order to list all the skills connected to your assistant`);
    process.exit(1);
} else {
    // Removing the skill manifest from the assistant skills array
    logger.warning(`Removing the '${args.skillName}' skill from your assistant's skills configuration file.`);
    assistantSkills.splice(assistantSkills.indexOf(skillToRemove), 1);

    // Writing (and overriding) the assistant skills file
    writeFileSync(assistantSkillsPath, JSON.stringify(assistantSkills, undefined, 4));
    logger.success(`Successfully removed '${args.skillName}' skill from your assistant's skills configuration file.`);
}

process.exit(0);
