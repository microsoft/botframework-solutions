/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { existsSync, readFileSync } from 'fs';
import { extname, isAbsolute, join, resolve } from 'path';
import { UpdateSkill } from './functionality';
import { ConsoleLogger, ILogger } from './logger';
import { IAppSetting, ICognitiveModel, IUpdateConfiguration } from './models';
import { sanitizePath, validatePairOfArgs } from './utils';

function showErrorHelp(): void {
    program.outputHelp((str: string) => {
        logger.error(str);

        return '';
    });
    process.exit(1);
}

const logger: ILogger = new ConsoleLogger();

program.Command.prototype.unknownOption = (flag: string): void => {
    logger.error(`Unknown arguments: ${flag}`);
    showErrorHelp();
};

// tslint:disable: max-line-length
program
    .name('botskills update')
    .description('Update a specific skill from your assistant bot.')
    .option('-l, --localManifest <path>', 'Path to local Skill Manifest file')
    .option('-r, --remoteManifest <url>', 'URL to remote Skill Manifest')
    .option('--cs', 'Determine your assistant project structure to be a CSharp-like structure')
    .option('--ts', 'Determine your assistant project structure to be a TypeScript-like structure')
    .option('--noRefresh', '[OPTIONAL] Determine whether the model of your skills connected are not going to be refreshed (by default they are refreshed)')
    .option('--dispatchName [name]', '[OPTIONAL] Name of your assistant\'s \'.dispatch\' file (defaults to the name displayed in your Cognitive Models file)')
    .option('--language [language]', '[OPTIONAL] Locale used for LUIS culture (defaults to \'en-us\')')
    .option('--luisFolder [path]', '[OPTIONAL] Path to the folder containing your Skills\' .lu files (defaults to \'./deployment/resources/skills/en\' inside your assistant folder)')
    .option('--dispatchFolder [path]', '[OPTIONAL] Path to the folder containing your assistant\'s \'.dispatch\' file (defaults to \'./deployment/resources/dispatch/en\' inside your assistant folder)')
    .option('--outFolder [path]', '[OPTIONAL] Path for any output file that may be generated (defaults to your assistant\'s root folder)')
    .option('--lgOutFolder [path]', '[OPTIONAL] Path for the LuisGen output (defaults to a \'service\' folder inside your assistant\'s folder)')
    .option('--skillsFile [path]', '[OPTIONAL] Path to your assistant Skills configuration file (defaults to the \'skills.json\' inside your assistant\'s folder)')
    .option('--resourceGroup [path]', '[OPTIONAL] Name of your assistant\'s resource group in Azure (defaults to your assistant\'s bot name)')
    .option('--appSettingsFile [path]', '[OPTIONAL] Path to your app settings file (defaults to \'appsettings.json\' inside your assistant\'s folder)')
    .option('--cognitiveModelsFile [path]', '[OPTIONAL] Path to your Cognitive Models file (defaults to \'cognitivemodels.json\' inside your assistant\'s folder)')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
    process.exit(0);
}

const skillId: string = '';
let botName: string = '';
let localManifest: string;
let remoteManifest: string;
let noRefresh: boolean = false;
let dispatchName: string;
let language: string;
let luisFolder: string;
let dispatchFolder: string;
let outFolder: string;
let lgOutFolder: string;
let skillsFile: string = '';
let resourceGroup: string = '';
let appSettingsFile: string;
let cognitiveModelsFile: string;
let lgLanguage: string;

logger.isVerbose = args.verbose;

// Validation of arguments
// cs and ts validation
const csAndTsValidationResult: string = validatePairOfArgs(args.cs, args.ts);
if (csAndTsValidationResult) {
    logger.error(
        csAndTsValidationResult.replace('{0}', 'cs')
        .replace('{1}', 'ts')
    );
    process.exit(1);
}

lgLanguage = args.cs ? 'cs' : 'ts';

// noRefresh validation
if (args.noRefresh) {
    noRefresh = true;
}

// localManifest && remoteManifest validation
const manifestValidationResult: string = validatePairOfArgs(args.localManifest, args.remoteManifest);
if (manifestValidationResult) {
    logger.error(
        manifestValidationResult.replace('{0}', 'localManifest')
        .replace('{1}', 'remoteManifest')
    );
    process.exit(1);
}
if (args.localManifest && extname(args.localManifest) !== '.json') {
    logger.error(`The 'localManifest' argument should be a path to a JSON file.`);
    process.exit(1);
}

localManifest = args.localManifest;
remoteManifest = args.remoteManifest;

// outFolder validation -- the const is needed for reassuring 'configuration.outFolder' is not undefined
outFolder = args.outFolder ? sanitizePath(args.outFolder) : resolve('./');

// skillsFile validation
if (!args.skillsFile) {
    skillsFile = join(outFolder, (args.ts ? join('src', 'skills.json') : 'skills.json'));
} else if (extname(args.skillsFile) !== '.json') {
    logger.error(`The 'skillsFile' argument should be a JSON file.`);
    process.exit(1);
} else {
    const skillsFilePath: string = isAbsolute(args.skillsFile) ? args.skillsFile : join(resolve('./'), args.skillsFile);
    if (!existsSync(skillsFilePath)) {
        logger.error(`The 'skillsFile' argument leads to a non-existing file.
            Please make sure to provide a valid path to your Assistant Skills configuration file using the '--skillsFile' argument.`);
        process.exit(1);
    }
    skillsFile = skillsFilePath;
}

// appSettingsFile validation
appSettingsFile = args.appSettingsFile || join(outFolder, (args.ts ? join('src', 'appsettings.json') : 'appsettings.json'));

if (appSettingsFile !== undefined) {
    const appSettings: IAppSetting = JSON.parse(readFileSync(appSettingsFile, 'UTF8'));
    botName = appSettings.botWebAppName;
    resourceGroup = appSettings.resourceGroupName;
} else {
    logger.error(`The 'appSettings' file doesn't exist`);
    process.exit(1);
}

// cognitiveModelsFile validation
const cognitiveModelsFilePath: string = args.cognitiveModelsFile || join(outFolder, (args.ts ? join('src', 'cognitivemodels.json') : 'cognitivemodels.json'));
cognitiveModelsFile = cognitiveModelsFilePath;

// language validation
language = args.language || 'en-us';
const languageCode: string = (language.split('-'))[0];

// luisFolder validation
luisFolder = args.luisFolder ? sanitizePath(args.luisFolder) : join(outFolder, 'Deployment', 'Resources', 'Skills', languageCode);

// dispatchFolder validation
dispatchFolder = args.dispatchFolder ? sanitizePath(args.dispatchFolder) : join(outFolder, 'Deployment', 'Resources', 'Dispatch', languageCode);

// lgOutFolder validation
lgOutFolder = args.lgOutFolder ? sanitizePath(args.lgOutFolder) : join(outFolder, (args.ts ? join('src', 'Services') : 'Services'));

// dispatchName validation
if (!args.dispatchName) {
    // try get the dispatch name from the cognitiveModels file
    const cognitiveModels: ICognitiveModel = JSON.parse(readFileSync(cognitiveModelsFilePath, 'UTF8'));
    dispatchName = cognitiveModels.cognitiveModels[languageCode].dispatchModel.name;
} else {
    dispatchName = args.dispatchName;
}

// End of arguments validation

// Initialize an instance of IUpdateConfiguration to send the needed arguments to the updateSkill function
const configuration: IUpdateConfiguration = {
    skillId: skillId,
    botName: botName,
    localManifest: localManifest,
    remoteManifest: remoteManifest,
    noRefresh: noRefresh,
    dispatchName: dispatchName,
    language: language,
    luisFolder: luisFolder,
    dispatchFolder: dispatchFolder,
    outFolder: outFolder,
    lgOutFolder: lgOutFolder,
    skillsFile: skillsFile,
    resourceGroup: resourceGroup,
    appSettingsFile: appSettingsFile,
    cognitiveModelsFile: cognitiveModelsFile,
    lgLanguage: lgLanguage,
    logger: logger
};

new UpdateSkill(logger).updateSkill(configuration);
