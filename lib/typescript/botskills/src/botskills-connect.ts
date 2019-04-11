/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { existsSync, writeFileSync } from 'fs';
import { extname, isAbsolute, join, resolve } from 'path';
import { get } from 'request-promise-native';
import { ConsoleLogger, ILogger} from './logger/logger';
import { ISkillManifest } from './skillManifest';

function showErrorHelp(): void {
    program.outputHelp((str: string) => {
        logger.error(str);

        return '';
    });
    process.exit(1);
}

async function getRemoteManifest(resourcePath: string): Promise<ISkillManifest> {
    return get({
        uri: <string> resourcePath,
        json: true
    });
}

function getLocalManifest(resourcePath: string): ISkillManifest {
    const skillManifestPath: string = isAbsolute(resourcePath) ? resourcePath : join(resolve('./'), resourcePath);

    if (!existsSync(skillManifestPath)) {
        logger.error(
            `The 'skillManifest' argument leads to a non-existing file. Please make sure to provide a valid path to your Skill manifest.`);
        process.exit(1);
    }

    // tslint:disable-next-line: non-literal-require
    return require(skillManifestPath);
}

function validateManifestSchema(skillManifest: ISkillManifest): void {
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
}

const logger: ILogger = new ConsoleLogger();

program.Command.prototype.unknownOption = (flag: string): void => {
    logger.error(`Unknown arguments: ${flag}`);
    showErrorHelp();
};

program
    .name('botskills connect')
    .description('Connect the skill to your assistant bot')
    .option('-l, --localResource <path>', 'Path to local Skill Manifest file')
    .option('-r, --remoteResource <path>', 'URL to remote Skill Manifest')
    .option('-a, --assistantSkills <path>', 'Path to assistant Skills configuration file')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
    process.exit(0);
}

async function connectSkill(): Promise<void> {
    logger.isVerbose = args.verbose;

    // Validation of arguments
    // localResource && remoteResource validation
    if (!args.localResource && !args.remoteResource) {
        logger.error(`One of the arguments 'localResource' or 'remoteResource' should be provided.`);
        process.exit(1);
    } else if (args.localResource && args.remoteResource) {
        logger.error(`Only one of the arguments 'localResource' or 'remoteResource' should be provided.`);
        process.exit(1);
    } else if (args.localResource && extname(args.localResource) !== '.json') {
        logger.error(`The 'localResource' argument should be a path to a JSON file.`);
        process.exit(1);
    }

    // assistantSkills validation
    if (!args.assistantSkills) {
        logger.error(`The 'assistantSkills' argument should be provided.`);
        process.exit(1);
    } else if (extname(args.assistantSkills) !== '.json') {
        logger.error(`The 'assistantSkills' argument should be a JSON file.`);
        process.exit(1);
    }
    const assistantSkillsPath: string = isAbsolute(args.assistantSkills) ? args.assistantSkills : join(resolve('./'), args.assistantSkills);
    if (!existsSync(assistantSkillsPath)) {
        logger.error(`The 'assistantSkills' argument leads to a non-existing file.
    Please make sure to provide a valid path to your Assistant Skills configuration file.`);
        process.exit(1);
    }
    // End of arguments validation

    // Take skillManifest
    const skillManifest: ISkillManifest = args.localResource
    ? getLocalManifest(args.localResource)
    : await getRemoteManifest(args.remoteResource);

    // Manifest schema validation
    validateManifestSchema(skillManifest);

    if (logger.isError) {
        process.exit(1);
    }
    // End of manifest schema validation

    // Take VA Skills configurations
    //tslint:disable-next-line: no-var-requires non-literal-require
    const assistantSkills: ISkillManifest[] = require(assistantSkillsPath);

    // Check if the skill is already connected to the assistant
    if (assistantSkills.find((assistantSkill: ISkillManifest) => assistantSkill.id === skillManifest.id)) {
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
}

connectSkill();
