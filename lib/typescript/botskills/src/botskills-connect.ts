/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { existsSync, writeFileSync } from 'fs';
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

function getManifest(): ISkillManifest {
    // Determine wether the manifest will be taken locally or remotely

    // Remote manifest
    // PENDING

    // Local manifest
    const skillManifestPath: string = isAbsolute(args.skillManifest) ? args.skillManifest : join(resolve('./'), args.skillManifest);
    if (!existsSync(skillManifestPath)) {
        logger.error(
            `The 'skillManifest' argument leads to a non-existing file. Please make sure to provide a valid path to your Skill manifest.`);
        process.exit(1);
    }

    // tslint:disable-next-line: non-literal-require
    return require(skillManifestPath);
}

const logger: ILogger = new ConsoleLogger();

program.Command.prototype.unknownOption = (flag: string): void => {
    logger.error(`Unknown arguments: ${flag}`);
    showErrorHelp();
};

program
    .name('botskills connect')
    .description('Connect the skill to your assistant bot')
    .option('-m, --skillManifest <path>', 'Path to Skill Manifest')
    .option('-a, --assistantSkills <path>', 'Path to Virtual Assistant\'s Skills')
    .option('--verbose', '[OPTIONAL] Outputs detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
    process.exit(0);
}

logger.isVerbose = args.verbose;

// Validation of arguments
// skillManifest validation
if (!args.skillManifest) {
    logger.error(`The 'skillManifest' argument should be provided.`);
    process.exit(1);
} else if (args.skillManifest.substring(args.skillManifest.lastIndexOf('.') + 1) !== 'json') {
    logger.error(`The 'skillManifest' argument should be a JSON file.`);
    process.exit(1);
}

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

// Take skillManifest
const skillManifest: ISkillManifest = getManifest();
if (!skillManifest.name) {
    logger.error(`Missing property 'name' of the manifest`);
}
if (!skillManifest.id) {
    logger.error(`Missing property 'id' of the manifest`);
}
if (!skillManifest.endpoint) {
    logger.error(`Missing property 'endpoint' of the manifest`);
}
if (!skillManifest.authenticationConnections) {
    logger.error(`Missing property 'authenticationConnections' of the manifest`);
}
if (!skillManifest.actions || !skillManifest.actions[0]) {
    logger.error(`Missing property 'actions' of the manifest`);
}

if (logger.isError) {
    process.exit(1);
}

// Take VA Skills configurations
//tslint:disable-next-line: no-var-requires non-literal-require
const assistantSkills: ISkillManifest[] = require(assistantSkillsPath);
// Check if the skill is already connected to the assistant
if (assistantSkills.find((assistantSkill: ISkillManifest) => assistantSkill.name === skillManifest.name)) {
    logger.warning(`The skill '${skillManifest.name}' is already registered.`);
    process.exit(1);
}
// Adding the skill manifest to the assistant skills array
logger.warning(`Appending '${skillManifest.name}' manifest to your assistant's skills configuration file.`);
assistantSkills.push(skillManifest);
// Writing (and overriding) the assistant skills file
writeFileSync(assistantSkillsPath, JSON.stringify(assistantSkills, undefined, 4));
logger.success(`Successfully appended '${skillManifest.name}' manifest to your assistant's skills configuration file!`);

process.exit(0);
