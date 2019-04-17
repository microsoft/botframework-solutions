/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as child_process from 'child_process';
import * as program from 'commander';
import { existsSync, writeFileSync } from 'fs';
import { extname, isAbsolute, join, resolve } from 'path';
import { get } from 'request-promise-native';
import * as util from 'util';
import { ConsoleLogger, ILogger} from './logger/logger';
import { IAction, ISkillManifest, IUtteranceSource } from './models';

async function execDispatch(args: string[]): Promise<string> {
    const dispatchPath: string = join(__dirname, '..', 'node_modules', 'botdispatch', 'bin', 'netcoreapp2.1', 'Dispatch.dll');
    return new Promise((pResolve, pReject) => {
        child_process.spawn('dotnet', [ dispatchPath, ...args], { stdio: 'inherit' })
        .on('close', (code: number) => {
            pResolve('');
        })
        .on('error', (err: Error) => {
            pReject(err);
        });
    });
}

async function spawn(command: string, args: string[]): Promise<string> {
    return new Promise((pResolve, pReject) => {
        child_process.spawn(command, args, { stdio: 'inherit', env: process.env, argv0: command, cwd: join(__dirname, '..') })
        .on('close', (code: number) => {
            pResolve('');
        })
        .on('error', (err: Error) => {
            pReject(err);
        });
    });
}

async function execute(command: string, args: string[]): Promise<string> {
    if (command === 'dispatch') {
        return execDispatch(args);
    }
    
    return new Promise((pResolve, pReject) => {
        child_process.exec(command + ' ' + args.join(' '), (err, stdout, stderr) => {
            if (stderr) pReject(stderr);
            pResolve(stdout);
        });
    });
}

// tslint:disable-next-line: no-any
async function runCommand(command: string, description: string): Promise<any> {
    logger.command(description, command);
    // tslint:disable-next-line: no-any
    const parts: string[] = command.split(' ');
    const cmd: string = parts[0];
    const args: string[] = parts.slice(1).filter(arg => arg);

    try {
        const result: string = await execute(cmd, args);
        return result;
    } catch (err) {
        return err;
    }
}

function showErrorHelp(): void {
    program.outputHelp((str: string) => {
        logger.error(str);

        return '';
    });
    process.exit(1);
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

async function updateDispatch(manifest: ISkillManifest): Promise<void> {
    // Initializing variables for the updateDispatch scope
    const dispatchFile: string = `${args.dispatchName}.dispatch`;
    const dispatchJsonFile: string = `${args.dispatchName}.json`;
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
        let ludownParseCommand: string = `ludown parse toluis `;
        ludownParseCommand += `--in ${join(args.luisFolder, luFile)} `;
        ludownParseCommand += `--luis_culture ${args.language} `;
        ludownParseCommand += `--out_folder ${args.luisFolder} `; //luisFolder should point to 'en' folder inside LUIS folder
        ludownParseCommand += `--out "${luisApp}.luis" `;

        let dispatchAddCommand: string = `dispatch add `;
        dispatchAddCommand += `--type file `;
        dispatchAddCommand += `--name ${args.dispatchName} `;
        dispatchAddCommand += `--filePath ${join(args.luisFolder, luisFile)} `;
        dispatchAddCommand += `--intentName ${intentName} `;
        dispatchAddCommand += `--dataFolder ${args.dispatchFolder} `;
        dispatchAddCommand += `--dispatch ${join(args.dispatchFolder, dispatchFile)} `;

        logger.message(await runCommand(ludownParseCommand, `Parsing ${luisApp} LU file`));
        logger.message(await runCommand(dispatchAddCommand, `Executing dispatch add for the ${luisApp} LU file`));
    }));

    logger.message('Running dispatch refresh...');

    let dispatchRefreshCommand: string = `dispatch refresh `;
    dispatchRefreshCommand += `--dispatch ${join(args.dispatchFolder, dispatchFile)} `;
    dispatchRefreshCommand += `--dataFolder ${args.dispatchFolder} `;

    await runCommand(dispatchRefreshCommand, `Executing dispatch refresh for the ${args.dispatchName} file`);

    logger.message('Running LuisGen...');

    let luisgenCommand: string = `luisgen `;
    luisgenCommand += `${join(args.dispatchFolder, dispatchJsonFile)} `;
    luisgenCommand += `-cs "DispatchLuis `;
    luisgenCommand += `-o ${args.lgOutFolder} `;

    await runCommand(luisgenCommand, `Executing luisgen for the ${args.dispatchName} file`);
}

async function connectSkill(): Promise<void> {
    logger.isVerbose = args.verbose;

    // Validation of arguments
    // localManifest && remoteManifest validation
    if (!args.localManifest && !args.remoteManifest) {
        logger.error(`One of the arguments 'localManifest' or 'remoteManifest' should be provided.`);
        process.exit(1);
    } else if (args.localManifest && args.remoteManifest) {
        logger.error(`Only one of the arguments 'localManifest' or 'remoteManifest' should be provided.`);
        process.exit(1);
    } else if (args.localManifest && extname(args.localManifest) !== '.json') {
        logger.error(`The 'localManifest' argument should be a path to a JSON file.`);
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

    // dispatchName validation
    if (!args.dispatchName) {
        logger.error(`The 'dispatchName' argument should be provided.`);
        process.exit(1);
    }

    // language validation
    if (!args.language) {
        args.language = 'en-us';
        logger.warning(`The 'language' argument was not provided. Its default value will be used instead.`);
    }

    // luisFolder validation
    if (!args.luisFolder) {
        args.luisFolder = join(resolve('./'), '..', 'resources', 'skills', args.language.split('-')[0]);
        logger.warning(`The 'luisFolder' argument was not provided. Its default value will be used instead.`);
    }

    // dispatchFolder validation
    if (!args.dispatchFolder) {
        args.dispatchFolder = join(resolve('./'), '..', 'resources', 'dispatch', args.language.split('-')[0]);
        logger.warning(`The 'dispatchFolder' argument was not provided. Its default value will be used instead.`);
    }

    // outFolder validation
    if (!args.outFolder) {
        args.outFolder = resolve('./');
        logger.warning(`The 'outFolder' argument was not provided. Its default value will be used instead.`);
    }

    // lgOutFolder validation
    if (!args.lgOutFolder) {
        args.lgOutFolder = join(args.outFolder, 'services');
        logger.warning(`The 'lgOutFolder' argument was not provided. Its default value will be used instead.`);
    }

    // End of arguments validation

    // Take skillManifest
    const skillManifest: ISkillManifest = args.localManifest
    ? getLocalManifest(args.localManifest)
    : await getRemoteManifest(args.remoteManifest);

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
    await updateDispatch(skillManifest);
}

const logger: ILogger = new ConsoleLogger();
// tslint:disable-next-line: no-any
const exec: any = util.promisify(child_process.exec);

const luisFolder: string = join('..', 'resources', 'skills', '$langCode');

program.Command.prototype.unknownOption = (flag: string): void => {
    logger.error(`Unknown arguments: ${flag}`);
    showErrorHelp();
};

// tslint:disable: max-line-length
program
    .name('botskills connect')
    .description('Connect the skill to your assistant bot')
    .option('-l, --localManifest <path>', 'Path to local Skill Manifest file')
    .option('-r, --remoteManifest <url>', 'URL to remote Skill Manifest')
    .option('-a, --assistantSkills <path>', 'Path to assistant Skills configuration file')
    .option('-d, --dispatchName <name>', 'Name of your assistant\'s \'.dispatch\' file')
    .option('--language [language]', '[OPTIONAL] Locale used for LUIS culture (defaults to \'en-us\')')
    .option('--luisFolder [path]', '[OPTIONAL] Path to the folder containing your Skills\' .lu files (defaults to \'./deployment/resources/skills/en\' inside your assistant folder)')
    .option('--dispatchFolder [path]', '[OPTIONAL] Path to the folder containing your assistant\'s \'.dispatch\' file (defaults to \'./deployment/resources/dispatch/en\' inside your assistant folder)')
    .option('-o, --outFolder [path]', '[OPTIONAL] Path for any output file that may be generated (defaults to your assistant\'s root folder)')
    .option('--lgOutFolder [path]', '[OPTIONAL] Path for the LuisGen output (defaults to a \'service\' folder inside your assistant\'s root folder)')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
    process.exit(0);
}

connectSkill();
