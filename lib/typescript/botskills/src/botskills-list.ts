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
import { validatePairOfArgs } from './utils';

function showErrorHelp(): void {
    program.outputHelp((str: string) => {
        logger.error(str);

        return '';
    });
    process.exit(1);
}

function checkSkillsFile(skillsFile: string): boolean {
    const skillsFilePath: string = isAbsolute(skillsFile) ? skillsFile : join(resolve('./'), skillsFile);
    if (!existsSync(skillsFilePath)) {

        return false;
    }

    return true;
}

const logger: ILogger = new ConsoleLogger();

program.Command.prototype.unknownOption = (flag: string): void => {
    logger.error(`Unknown arguments: ${flag}`);
    showErrorHelp();
};

program
    .name('botskills list')
    .description('List all the Skills connected to your assistant')
    .option('-f, --skillsFile <path>', 'Path to assistant Skills configuration file')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

logger.isVerbose = args.verbose;

// skillsFile validation
if (!args.skillsFile) {
    args.skillsFile = join('src', 'skills.json');
    if (!checkSkillsFile(args.skillsFile)) {
        args.skillsFile = 'skills.json';
        if (!checkSkillsFile(args.skillsFile)) {
            logger.error(`The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file.`);
            process.exit(1);
        }
    }
} else if (extname(args.skillsFile) !== '.json') {
    logger.error(`The 'skillsFile' argument should be a JSON file.`);
    process.exit(1);
} else {
    if (!checkSkillsFile(args.skillsFile)) {
        logger.error(`The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file.`);
        process.exit(1);
    }
}

const configuration: IListConfiguration = {
    skillsFile: isAbsolute(args.skillsFile) ? args.skillsFile : join(resolve('./'), args.skillsFile),
    logger: logger
};

listSkill(configuration);

process.exit(0);
