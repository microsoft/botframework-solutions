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

function getManifest(): ISkillManifest {
    // Determine wether the manifest will be taken locally or remotely

    // Remote manifest
    // PENDING

    // Local manifest
    const skillManifestPath: string = isAbsolute(args.skillManifest) ? args.skillManifest : join(resolve('./'), args.skillManifest);
    if (!existsSync(skillManifestPath)) {
        console.error(chalk.redBright(
        `The 'skillManifest' argument leads to a non-existing file. Please make sure to provide a valid path to your Skill manifest.`));
        process.exit(1);
    }

    // tslint:disable-next-line: non-literal-require
    return require(skillManifestPath);
}

program.Command.prototype.unknownOption = (flag: string): void => {
    console.error(chalk.redBright(`Unknown arguments: ${flag}`));
    showErrorHelp();
};

program
    .name('botskills connect')
    .description('Connect the skill to your assistant bot')
    .option('-m, --skillManifest <path>', 'path to Skill Manifest')
    .option('-a, --assistantSkills <path>', 'path to Virtual Assistant\'s Skills')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
} else {
    // Validation of arguments
    // skillManifest validation
    if (!args.skillManifest) {
        console.error(chalk.redBright(`The 'skillManifest' argument should be provided.`));
        process.exit(1);
    } else if (args.skillManifest.substring(args.skillManifest.lastIndexOf('.') + 1) !== 'json') {
        console.error(chalk.redBright(`The 'skillManifest' argument should be a JSON file.`));
        process.exit(1);
    }

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
        `The 'assistantSkills' argument leads to a non-existing file.`
        + `\nPlease make sure to provide a valid path to your Assistant Skills configuration file.`));
        process.exit(1);
    }

    // Take skillManifest
    const skillManifest: ISkillManifest = getManifest();
    if (!skillManifest.name) {
        console.error(chalk.redBright(`Missing property 'name' of the manifest`));
    }
    if (!skillManifest.id) {
        console.error(chalk.redBright(`Missing property 'id' of the manifest`));
    }
    if (!skillManifest.endpoint) {
        console.error(chalk.redBright(`Missing property 'endpoint' of the manifest`));
    }
    if (!skillManifest.authenticationConnections) {
        console.error(chalk.redBright(`Missing property 'authenticationConnections' of the manifest`));
    }
    if (!skillManifest.actions || !skillManifest.actions[0]) {
        console.error(chalk.redBright(`Missing property 'actions' of the manifest`));
    }

    // Take VA Skills configurations
    //tslint:disable-next-line: no-var-requires non-literal-require
    const assistantSkills: ISkillManifest[] = require(assistantSkillsPath);
    // Check if the skill is already connected to the assistant
    if (assistantSkills.find((assistantSkill: ISkillManifest) => assistantSkill.name === skillManifest.name)) {
        console.log(chalk.yellow(`The skill '${skillManifest.name}' is already registered.`));
        process.exit(1);
    }
    // Adding the skill manifest to the assistant skills array
    console.log(chalk.yellow(`Appending '${skillManifest.name}' manifest to your assistant's skills configuration file.`));
    assistantSkills.push(skillManifest);
    // Writing (and overriding) the assistant skills file
    writeFileSync(assistantSkillsPath, JSON.stringify(assistantSkills, undefined, 4));
    console.log(chalk.greenBright(`Successfully appended '${skillManifest.name}' manifest to your assistant's skills configuration file!`));
}

function showErrorHelp(): void {
    program.outputHelp((str: string) => {
        console.error(str);

        return '';
    });
    process.exit(1);
}
