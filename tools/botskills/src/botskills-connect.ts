/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { readFileSync } from 'fs';
import { extname, join, resolve } from 'path';
import { ConnectSkill } from './functionality';
import { ConsoleLogger, ILogger } from './logger';
import { IAppSetting, IConnectConfiguration } from './models';
import { sanitizePath, validatePairOfArgs } from './utils';

const logger: ILogger = new ConsoleLogger();

function showErrorHelp(): void {
    program.outputHelp((str: string): string => {
        logger.error(str);

        return '';
    });
    process.exit(1);
}


program.Command.prototype.unknownOption = (flag: string): void => {
    logger.error(`Unknown arguments: ${ flag }`);
    showErrorHelp();
};

program
    .name('botskills connect')
    .description('Connect a skill to your assistant bot. Only one of both path or URL to Skill is needed.')
    .option('-l, --localManifest <path>', 'Path to local Skill Manifest file')
    .option('-r, --remoteManifest <url>', 'URL to remote Skill Manifest')
    .option('--cs', 'Determine your assistant project structure to be a CSharp-like structure')
    .option('--ts', 'Determine your assistant project structure to be a TypeScript-like structure')
    .option('--noRefresh [true|FALSE]', '[OPTIONAL] Determine whether the model of your skills connected are not going to be refreshed (by default they are refreshed)')
    .option('-e, --endpointName <name>', '[OPTIONAL] Name of the endpoint to connect to your assistant (case sensitive)(defaults to using the first endpoint)')
    .option('--languages [languages]', '[OPTIONAL] Comma separated list of locales used for LUIS culture (defaults to \'en-us\')')
    .option('--luisFolder [path]', '[OPTIONAL] Path to the folder containing your Skills\' .lu files (defaults to \'./deployment/resources/skills\' inside your assistant folder)')
    .option('--dispatchFolder [path]', '[OPTIONAL] Path to the folder containing your assistant\'s \'.dispatch\' file (defaults to \'./deployment/resources/dispatch\' inside your assistant folder)')
    .option('--outFolder [path]', '[OPTIONAL] Path for any output file that may be generated (defaults to your assistant\'s root folder)')
    .option('--lgOutFolder [path]', '[OPTIONAL] Path for the Luis Generate output (defaults to a \'service\' folder inside your assistant\'s folder)')
    .option('--resourceGroup [path]', '[OPTIONAL] Name of your assistant\'s resource group in Azure (defaults to your assistant\'s bot name)')
    .option('--appSettingsFile [path]', '[OPTIONAL] Path to your app settings file (defaults to \'appsettings.json\' inside your assistant\'s folder)')
    .option('--cognitiveModelsFile [path]', '[OPTIONAL] Path to your Cognitive Models file (defaults to \'cognitivemodels.json\' inside your assistant\'s folder)')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command): undefined => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
    process.exit(0);
}

let botName = '';
let localManifest: string;
let remoteManifest: string;
let endpointName: string;
let noRefresh = false;
let languages: string[];
let luisFolder: string;
let dispatchFolder: string;
let outFolder: string;
let lgOutFolder: string;
let resourceGroup = '';
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
endpointName = args.endpointName;

// outFolder validation -- the var is needed for reassuring 'configuration.outFolder' is not undefined
outFolder = args.outFolder ? sanitizePath(args.outFolder) : resolve('./');

// appSettingsFile validation
appSettingsFile = args.appSettingsFile || join(outFolder, (args.ts ? join('src', 'appsettings.json') : 'appsettings.json'));

// validate the existence of the appsettings file
if (appSettingsFile !== undefined) {
    const appSettings: IAppSetting = JSON.parse(readFileSync(appSettingsFile, 'UTF8'));
    // use botWebAppName and resourceGroupName properties from appsettings file
    botName = appSettings.botWebAppName;
    resourceGroup = appSettings.resourceGroupName;
} else {
    logger.error(`The 'appSettings' file doesn't exist`);
    process.exit(1);
}

// cognitiveModelsFile validation
const cognitiveModelsFilePath: string = args.cognitiveModelsFile || join(outFolder, (args.ts ? join('src', 'cognitivemodels.json') : 'cognitivemodels.json'));
cognitiveModelsFile = cognitiveModelsFilePath;

// languages validation
languages = args.languages ? args.languages.split(',') : ['en-us'];

// luisFolder validation
luisFolder = args.luisFolder ? sanitizePath(args.luisFolder) : join(outFolder, 'Deployment', 'Resources', 'Skills');

// dispatchFolder validation
dispatchFolder = args.dispatchFolder ? sanitizePath(args.dispatchFolder) : join(outFolder, 'Deployment', 'Resources', 'Dispatch');

// lgOutFolder validation
lgOutFolder = args.lgOutFolder ? sanitizePath(args.lgOutFolder) : join(outFolder, (args.ts ? join('src', 'Services', 'DispatchLuis.ts') : join('Services', 'DispatchLuis.cs')));

// Initialize an instance of IConnectConfiguration to send the needed arguments to the connectSkill function
const configuration: IConnectConfiguration = {
    botName: botName,
    localManifest: localManifest,
    remoteManifest: remoteManifest,
    endpointName: endpointName,
    noRefresh: noRefresh,
    languages: languages,
    luisFolder: luisFolder,
    dispatchFolder: dispatchFolder,
    outFolder: outFolder,
    lgOutFolder: lgOutFolder,
    resourceGroup: resourceGroup,
    appSettingsFile: appSettingsFile,
    cognitiveModelsFile: cognitiveModelsFile,
    lgLanguage: lgLanguage,
    logger: logger
};

// End of arguments validation
new ConnectSkill((configuration as IConnectConfiguration), logger).connectSkill();
