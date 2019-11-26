/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { existsSync } from 'fs';
import { extname, isAbsolute, join, resolve } from 'path';
import { ListSkill } from './functionality';
import { ConsoleLogger, ILogger} from './logger';
import { IListConfiguration } from './models';

function showErrorHelp(): void {
    program.outputHelp((str: string): string => {
        logger.error(str);

        return '';
    });
    process.exit(1);
}

function existFile(file: string): boolean {
    const filePath: string = isAbsolute(file) ? file : join(resolve('./'), file);
    if (!existsSync(filePath)) {

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
    .option('-f, --skillsFile [path]', '[OPTIONAL] Path to assistant Skills configuration file')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command): undefined => undefined);

const args: program.Command = program.parse(process.argv);

let skillsFile: string = '';

logger.isVerbose = args.verbose;

// skillsFile validation
if (!args.skillsFile) {
    args.skillsFile = join('src', 'skills.json');
    if (!existFile(args.skillsFile)) {
        args.skillsFile = 'skills.json';
        if (!existFile(args.skillsFile)) {
            logger.error(`The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--skillsFile' argument.`);
            process.exit(1);
        }
    }
} else if (extname(args.skillsFile) !== '.json') {
    logger.error(`The 'skillsFile' argument should be a JSON file.`);
    process.exit(1);
} else {
    if (!existFile(args.skillsFile)) {
        logger.error(`The 'skillsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--skillsFile' argument.`);
        process.exit(1);
    }
}

skillsFile = isAbsolute(args.skillsFile) ? args.skillsFile : join(resolve('./'), args.skillsFile);

// Initialize an instance of IListConfiguration to send the needed arguments to the listSkill function
const configuration: IListConfiguration = {
    skillsFile: skillsFile,
    logger: logger
};
new ListSkill(logger).listSkill(configuration);

process.exit(0);
