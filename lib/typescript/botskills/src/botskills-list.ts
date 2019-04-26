/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { existsSync } from 'fs';
import { extname, isAbsolute, join, resolve } from 'path';
import { listSkill } from './functionality';
import { ConsoleLogger, ILogger} from './logger';
import { IListConfiguration } from './models';

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
    .option('-f, --skillsFile <path>', 'Path to assistant Skills configuration file')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
    process.exit(0);
}

logger.isVerbose = args.verbose;

// skillsFile validation
if (!args.skillsFile) {
    logger.error(`The 'skillsFile' argument should be provided.`);
    process.exit(1);
} else if (extname(args.skillsFile) !== '.json') {
    logger.error(`The 'skillsFile' argument should be a JSON file.`);
    process.exit(1);
}

const skillsFilePath: string = isAbsolute(args.skillsFile) ? args.skillsFile : join(resolve('./'), args.skillsFile);
if (!existsSync(skillsFilePath)) {
    logger.error(`The 'skillsFile' argument leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file.`);
    process.exit(1);
}

const configuration: IListConfiguration = {
    skillsFile: skillsFilePath,
    logger: logger
};

listSkill(configuration);

process.exit(0);
