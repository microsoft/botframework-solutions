/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { existsSync } from 'fs';
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
    .name('botskills list')
    .description('Connect the skill to your assistant bot')
    .option('-a, --assistantSkills <path>', 'Path to assistant Skills configuration file')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
    process.exit(0);
}

logger.isVerbose = args.verbose;

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
    logger.error(`The 'assistantSkills' argument leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file.`);
    process.exit(1);
}

// Take VA Skills configurations
//tslint:disable-next-line: no-var-requires non-literal-require
const assistantSkills: ISkillManifest[] = require(assistantSkillsPath);
let message: string = `The skills already connected to the assistant are the following:`;
assistantSkills.forEach((skillManifest: ISkillManifest) => {
    message += `\n\t- ${skillManifest.name}`;
});
logger.message(message);

process.exit(0);
