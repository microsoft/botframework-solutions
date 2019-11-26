#!/usr/bin/env node
/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { readFileSync } from 'fs';
import { join } from 'path';
import * as process from 'process';
import * as semver from 'semver';
import { ConsoleLogger, ILogger} from './logger/logger';

const logger: ILogger = new ConsoleLogger();
const pkg: IPackage = JSON.parse(readFileSync(join(__dirname, '..', 'package.json'), 'UTF8'));

const requiredVersion: string = pkg.engines.node;
if (!semver.satisfies(process.version, requiredVersion)) {
    logger.error(`Required node version ${requiredVersion} not satisfied with current version ${process.version}.`);
    process.exit(1);
}

program.Command.prototype.unknownOption = (flag: string): void => {
    logger.error(`Unknown arguments: ${flag}`);
    program.outputHelp((str: string): string => {
        logger.error(str);

        return '';
    });
    process.exit(1);
};

program
    .version(pkg.version, '-v, --version')
    .description(`The skill program makes it easy to manipulate skills for Microsoft Bot Framework tools.`);

program
    .command('connect', 'connect any skill to your assistant bot')
    .command('disconnect', 'disconnect a specific skill from your assitant bot')
    .command('update', 'update a specific skill from your assistant bot')
    .command('refresh', 'refresh the connected skills')
    .command('list', 'list the connected skills in the assistant');

const args: program.Command = program.parse(process.argv);
// args should be undefined is subcommand is executed
if (args !== undefined) {
    const unknownArgs: string[] = process.argv.slice(2);
    logger.error(`Unknown arguments: ${unknownArgs.join(' ')}`);
    program.outputHelp((str: string): string => {
        logger.error(str);

        return '';
    });
    process.exit(1);
}

/**
 * Interface for using typedef when requiring the package.json file
 */
interface IPackage {
    name: string;
    version: string;
    engines: { node: string };
}
