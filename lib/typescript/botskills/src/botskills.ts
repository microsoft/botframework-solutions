#!/usr/bin/env node
/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
// tslint:disable:no-console
// tslint:disable:no-object-literal-type-assertion
import * as chalk from 'chalk';
import * as program from 'commander';
import * as process from 'process';
import * as semver from 'semver';

// tslint:disable-next-line:no-var-requires no-require-imports
const pkg: IPackage = require('../package.json');

const requiredVersion: string = pkg.engines.node;
if (!semver.satisfies(process.version, requiredVersion)) {
    console.error(`Required node version ${requiredVersion} not satisfied with current version ${process.version}.`);
    process.exit(1);
}

program.Command.prototype.unknownOption = (flag: string): void => {
    console.error(chalk.default.redBright(`Unknown arguments: ${flag}`));
    program.outputHelp((str: string) => {
        console.error(str);

        return '';
    });
    process.exit(1);
};

program
    .version(pkg.version, '-v, --Version')
    .description(`The skill program makes it easy to manipulate skills for Microsoft Bot Framework tools.`);

program
    .command('connect', 'connect any skill to your assistant bot')
    .command('list', 'list the connected skills in the assistant');

const args: program.Command = program.parse(process.argv);
// args should be undefined is subcommand is executed
if (args) {
    const unknownArgs: string[] = process.argv.slice(2);
    console.error(chalk.default.redBright(`Unknown arguments: ${unknownArgs.join(' ')}`));
    program.outputHelp((str: string) => {
        console.error(str);

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
