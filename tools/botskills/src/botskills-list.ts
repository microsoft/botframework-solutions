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
    .name('botskills list')
    .description('List all the Skills connected to your assistant')
    .option('--appSettingsFile [path]', '[OPTIONAL] Path to your appsettings file (defaults to \'appsettings.json\' inside your assistant\'s folder)')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command): undefined => undefined);

const args: program.Command = program.parse(process.argv);

let appSettingsFile = '';

logger.isVerbose = args.verbose;

// appSettingsFile validation
if (!args.appSettingsFile) {
    args.appSettingsFile = join('src', 'appsettings.json');
    if (!existFile(args.appSettingsFile)) {
        args.appSettingsFile = 'appsettings.json';
        if (!existFile(args.appSettingsFile)) {
            logger.error(`The 'appSettingsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--appSettingsFile' argument.`);
            process.exit(1);
        }
    }
} else if (extname(args.appSettingsFile) !== '.json') {
    logger.error(`The 'appSettingsFile' argument should be a JSON file.`);
    process.exit(1);
} else {
    if (!existFile(args.appSettingsFile)) {
        logger.error(`The 'appSettingsFile' argument is absent or leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file using the '--appSettingsFile' argument.`);
        process.exit(1);
    }
}

appSettingsFile = isAbsolute(args.appSettingsFile) ? args.appSettingsFile : join(resolve('./'), args.appSettingsFile);

// Initialize an instance of IListConfiguration to send the needed arguments to the listSkill function
const configuration: IListConfiguration = {
    appSettingsFile: appSettingsFile,
    logger: logger
};
new ListSkill(logger).listSkill(configuration);

process.exit(0);
