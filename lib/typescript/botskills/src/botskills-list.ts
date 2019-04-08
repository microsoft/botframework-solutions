/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
// tslint:disable:no-console
import chalk from 'chalk';
import * as program from 'commander';
import { existsSync, writeFileSync } from 'fs';
import { isAbsolute, join, resolve } from 'path';
import { ISkillManifest } from './skillManifest';

program.Command.prototype.unknownOption = (flag: string): void => {
    console.error(chalk.redBright(`Unknown arguments: ${flag}`));
    showErrorHelp();
};

program
    .name('botskills list')
    .description('Connect the skill to your assistant bot')
    .option('-a, --assistantSkills <path>', 'path to Virtual Assistant\'s Skills')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
} else {
    // assistantSkills validation
    if (!args.assistantSkills) {
        console.error(chalk.redBright(`The 'assistantSkills' argument should be provided.`));
        process.exit(1);
    } else if (args.assistantSkills.substring(args.assistantSkills.lastIndexOf('.') + 1) !== 'json') {
        console.error(chalk.redBright(`The 'assistantSkills' argument should be a JSON file.`));
        process.exit(1);
    }
    const assistantSkillsPath: string = isAbsolute(args.assistantSkills) ? args.assistantSkills : join(resolve('./'), args.assistantSkills);
    if (!existsSync(assistantSkillsPath)) {
        console.error(chalk.redBright(
        `The 'assistantSkills' argument leads to a non-existing file.
Please make sure to provide a valid path to your Assistant Skills configuration file.`));
        process.exit(1);
    }

    // Take VA Skills configurations
    //tslint:disable-next-line: no-var-requires non-literal-require
    const assistantSkills: ISkillManifest[] = require(assistantSkillsPath);
    let message: string = `The skills already connected to the assistant are the following:`;
    assistantSkills.forEach((skillManifest) => {
        message += `\n\t- ${skillManifest.name}`;
    });
    console.log(chalk.white(message))
}

function showErrorHelp(): void {
    program.outputHelp((str: string) => {
        console.error(str);

        return '';
    });
    process.exit(1);
}
