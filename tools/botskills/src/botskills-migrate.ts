/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { existsSync } from 'fs';
import { extname, isAbsolute, join, resolve } from 'path';
import { MigrateSkill } from './functionality';
import { ConsoleLogger, ILogger} from './logger';
import { IMigrateConfiguration  } from './models';

const logger: ILogger = new ConsoleLogger();

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

program.Command.prototype.unknownOption = (flag: string): void => {
    logger.error(`Unknown arguments: ${ flag }`);
    showErrorHelp();
};

program
    .name('botskills migrate')
    .description('Migrate all the skills currently connected to your assistant to the new configuration settings (appSettings.json)')
    .option('--sourceFile [path]', '[OPTIONAL] Path to your skills.json file, which contains the skills that will be migrated (defaults to \'skills.json\' inside your Virtual Assistant\'s folder)')
    .option('--destFile [path]', '[OPTIONAL] Path to your appsettings file. The skills information will be migrated to this file (defaults to \'appsettings.json\' inside your Virtual Assistant\'s folder)')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command): undefined => undefined);

const args: program.Command = program.parse(process.argv);

let destFile = '';
let sourceFile = '';

logger.isVerbose = args.verbose;

// destFile validation
if (!args.destFile) {
    args.destFile = join('src', 'appsettings.json');
    if (!existFile(args.destFile)) {
        args.destFile = 'appsettings.json';
        if (!existFile(args.destFile)) {
            logger.error(`The 'destFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--destFile' argument.`);
            process.exit(1);
        }
    }
} else if (extname(args.destFile) !== '.json') {
    logger.error(`The 'destFile' argument should be a JSON file.`);
    process.exit(1);
} else {
    if (!existFile(args.destFile)) {
        logger.error(`The 'destFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--destFile' argument.`);
        process.exit(1);
    }
}

destFile = isAbsolute(args.destFile) ? args.destFile : join(resolve('./'), args.destFile);


// sourceFile validation
if (!args.sourceFile) {
    args.sourceFile = join('src', 'skills.json');
    if (!existFile(args.sourceFile)) {
        args.sourceFile = 'skills.json';
        if (!existFile(args.sourceFile)) {
            logger.error(`The 'sourceFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--sourceFile' argument.`);
            process.exit(1);
        }
    }
} else if (extname(args.sourceFile) !== '.json') {
    logger.error(`The 'sourceFile' argument should be a JSON file.`);
    process.exit(1);
} else {
    if (!existFile(args.sourceFile)) {
        logger.error(`The 'sourceFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--sourceFile' argument.`);
        process.exit(1);
    }
}

sourceFile = isAbsolute(args.sourceFile) ? args.sourceFile : join(resolve('./'), args.sourceFile);

// Initialize an instance of IMigrateConfiguration to send the needed arguments to the migrateSkill function
const configuration: IMigrateConfiguration = {
    destFile: destFile,
    logger: logger,
    sourceFile: sourceFile
};
new MigrateSkill(logger).migrateSkill(configuration);

process.exit(0);
