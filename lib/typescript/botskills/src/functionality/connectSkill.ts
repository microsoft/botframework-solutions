/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { existsSync, writeFileSync } from 'fs';
import { isAbsolute, join, resolve } from 'path';
import { get } from 'request-promise-native';
import { ConsoleLogger, ILogger} from '../logger';
import { IAction, IConnectConfiguration, ISkillFIle, ISkillManifest, IUtteranceSource } from '../models';
import { authenticate, execute } from '../utils';

async function runCommand(command: string[], description: string): Promise<string> {
    logger.command(description, command.join(' '));
    const cmd: string = command[0];
    const commandArgs: string[] = command.slice(1)
        .filter((arg: string) => arg);

    try {

        return await execute(cmd, commandArgs);
    } catch (err) {

        return err;
    }
}

async function getRemoteManifest(manifestUrl: string): Promise<ISkillManifest> {
    return get({
        uri: <string> manifestUrl,
        json: true
    });
}

function getLocalManifest(manifestPath: string): ISkillManifest {
    const skillManifestPath: string = isAbsolute(manifestPath) ? manifestPath : join(resolve('./'), manifestPath);

    if (!existsSync(skillManifestPath)) {
        logger.error(
            `The 'localManifest' argument leads to a non-existing file. Please make sure to provide a valid path to your Skill manifest.`);
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

async function updateDispatch(configuration: IConnectConfiguration, manifest: ISkillManifest): Promise<void> {
    // Initializing variables for the updateDispatch scope
    const dispatchFile: string = `${configuration.dispatchName}.dispatch`;
    const dispatchJsonFile: string = `${configuration.dispatchName}.json`;
    const intentName: string = manifest.id;
    let luisDictionary: Map<string, string>;

    logger.message('Getting intents for dispatch...');

    luisDictionary = manifest.actions.filter((action: IAction) => action.definition.triggers.utteranceSources)
    .reduce((acc: IUtteranceSource[], val: IAction) => acc.concat(val.definition.triggers.utteranceSources), [])
    .reduce((acc: string[], val: IUtteranceSource) => acc.concat(val.source), [])
    .reduce(
        (acc: Map<string, string>, val: string) => {
        const luis: string[] = val.split('#');
        if (acc.has(luis[0])) {
            const previous: string | undefined = acc.get(luis[0]);
            acc.set(luis[0], previous + luis[1]);
        } else {
            acc.set(luis[0], luis[1]);
        }

        return acc;
        },
        new Map());

    logger.message('Adding skill to Dispatch');

    await Promise.all(
    Array.from(luisDictionary.entries())
    .map(async(item: [string, string]) => {
        const luisApp: string = item[0];
        const luFile: string = `${luisApp}.lu`;
        const luisFile: string = `${luisApp}.luis`;

        // Parse LU file
        logger.message(`Parsing ${luisApp} LU file...`);
        const ludownParseCommand: string[] = ['ludown', 'parse', 'toluis'];
        ludownParseCommand.push(...['--in', join(configuration.luisFolder, luFile)]);
        ludownParseCommand.push(...['--luis_culture', configuration.language]);
        ludownParseCommand.push(...['--out_folder', configuration.luisFolder]); //luisFolder should point to 'en' folder inside LUIS folder
        ludownParseCommand.push(...['--out', `"${luisApp}.luis"`]);

        const dispatchAddCommand: string[] = ['dispatch', 'add'];
        dispatchAddCommand.push(...['--type', 'file']);
        dispatchAddCommand.push(...['--name', manifest.id]);
        dispatchAddCommand.push(...['--filePath', join(configuration.luisFolder, luisFile)]);
        dispatchAddCommand.push(...['--intentName', intentName]);
        dispatchAddCommand.push(...['--dataFolder', configuration.dispatchFolder]);
        dispatchAddCommand.push(...['--dispatch', join(configuration.dispatchFolder, dispatchFile)]);

        logger.message(await runCommand(ludownParseCommand, `Parsing ${luisApp} LU file`));
        logger.message(await runCommand(dispatchAddCommand, `Executing dispatch add for the ${luisApp} LU file`));
    }));

    logger.message('Running dispatch refresh...');

    const dispatchRefreshCommand: string[] = ['dispatch', 'refresh'];
    dispatchRefreshCommand.push(...['--dispatch', join(configuration.dispatchFolder, dispatchFile)]);
    dispatchRefreshCommand.push(...['--dataFolder', configuration.dispatchFolder]);

    await runCommand(dispatchRefreshCommand, `Executing dispatch refresh for the ${configuration.dispatchName} file`);

    logger.message('Running LuisGen...');

    const luisgenCommand: string[] = ['luisgen'];
    luisgenCommand.push(join(configuration.dispatchFolder, dispatchJsonFile));
    luisgenCommand.push(...[`-${configuration.lgLanguage}`, `"DispatchLuis"`]);
    luisgenCommand.push(...['-o', configuration.lgOutFolder]);

    await runCommand(luisgenCommand, `Executing luisgen for the ${configuration.dispatchName} file`);
}

export async function connectSkill(configuration: IConnectConfiguration): Promise<void> {

    if (configuration.logger) {
        logger = configuration.logger;
    }
    // Take skillManifest
    const skillManifest: ISkillManifest = configuration.localManifest
    ? getLocalManifest(configuration.localManifest)
    : await getRemoteManifest(configuration.remoteManifest);

    // Manifest schema validation
    validateManifestSchema(skillManifest);

    if (logger.isError) {
        process.exit(1);
    }
    // End of manifest schema validation

    // Take VA Skills configurations
    //tslint:disable-next-line: no-var-requires non-literal-require
    const assistantSkillsFile: ISkillFIle = require(configuration.skillsFile);
    const assistantSkills: ISkillManifest[] = assistantSkillsFile.skills || [];

    // Check if the skill is already connected to the assistant
    if (assistantSkills.find((assistantSkill: ISkillManifest) => assistantSkill.id === skillManifest.id)) {
        logger.warning(`The skill '${skillManifest.name}' is already registered.`);
        process.exit(1);
    }

    // Updating Dispatch
    logger.message('Updating Dispatch');
    await updateDispatch(configuration, skillManifest);

    // Adding the skill manifest to the assistant skills array
    logger.warning(`Appending '${skillManifest.name}' manifest to your assistant's skills configuration file.`);
    assistantSkills.push(skillManifest);

    // Updating the assistant skills file's skills property with the assistant skills array
    assistantSkillsFile.skills = assistantSkills;

    // Writing (and overriding) the assistant skills file
    writeFileSync(configuration.skillsFile, JSON.stringify(assistantSkillsFile, undefined, 4));
    logger.success(`Successfully appended '${skillManifest.name}' manifest to your assistant's skills configuration file!`);

    // Configuring bot auth settings
    logger.message('Configuring bot auth settings');
    await authenticate(configuration, skillManifest, logger);
}

let logger: ILogger = new ConsoleLogger();
