/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { join, resolve } from 'path';
import { DisconnectSkill } from './functionality';
import { ConsoleLogger, ILogger} from './logger';
import { IDisconnectConfiguration } from './models';
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
    .name('botskills disconnect')
    .description('Disconnect a specific skill from your assistant bot. The id of the Skill is needed.')
    .option('-i, --skillId <id>', 'Id of the skill to remove from your assistant (case sensitive)')
    .option('--cs', 'Determine your assistant project structure to be a CSharp-like structure')
    .option('--ts', 'Determine your assistant project structure to be a TypeScript-like structure')
    .option('--noRefresh', '[OPTIONAL] Determine whether the model of your skills connected are not going to be refreshed (by default they are refreshed)')
    .option('--languages [languages]', '[OPTIONAL] Comma separated list of locales used for LUIS culture (defaults to \'en-us\')')
    .option('--dispatchFolder [path]', '[OPTIONAL] Path to the folder containing your assistant\'s \'.dispatch\' file (defaults to \'./deployment/resources/dispatch\' inside your assistant folder)')
    .option('--outFolder [path]', '[OPTIONAL] Path for any output file that may be generated (defaults to your assistant\'s root folder)')
    .option('--lgOutFolder [path]', '[OPTIONAL] Path for the Luis Generate output (defaults to a \'service\' folder inside your assistant\'s folder)')
    .option('--appSettingsFile [path]', '[OPTIONAL] Path to your appsettings file (defaults to \'appsettings.json\' inside your assistant\'s folder)')
    .option('--cognitiveModelsFile [path]', '[OPTIONAL] Path to your Cognitive Models file (defaults to \'cognitivemodels.json\' inside your assistant\'s folder)')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command): undefined => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
    process.exit(0);
}

let skillId = '';
let outFolder: string;
let noRefresh = false;
let cognitiveModelsFile: string;
let languages: string[];
let dispatchFolder: string;
let lgOutFolder: string;
let lgLanguage: string;
let appSettingsFile: string;

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

// skillId validation
if (!args.skillId) {
    logger.error(`The 'skillId' argument should be provided.`);
    process.exit(1);
}

skillId = args.skillId;
// outFolder validation -- the var is needed for reassuring 'configuration.outFolder' is not undefined
outFolder = args.outFolder ? sanitizePath(args.outFolder) : resolve('./');

// appSettingsFile validation
appSettingsFile = args.appSettingsFile || join(outFolder, (args.ts ? join('src', 'appsettings.json') : 'appsettings.json'));

// validate the existence of the appsettings file
if (appSettingsFile === undefined) {
    logger.error(`The 'appSettings' file doesn't exist`);
    process.exit(1);
}

// cognitiveModelsFile validation
const cognitiveModelsFilePath: string = args.cognitiveModelsFile || join(
    outFolder, (args.ts ? join('src', 'cognitivemodels.json') : 'cognitivemodels.json'));
cognitiveModelsFile = cognitiveModelsFilePath;

// languages validation
languages = args.languages ? args.languages.split(',') : ['en-us'];

// dispatchFolder validation
dispatchFolder = args.dispatchFolder ?
    sanitizePath(args.dispatchFolder) : join(outFolder, 'Deployment', 'Resources', 'Dispatch');

// lgOutFolder validation
lgOutFolder = args.lgOutFolder ?
    sanitizePath(args.lgOutFolder) : join(outFolder, (args.ts ? join('src', 'Services', 'DispatchLuis.ts') : join('Services', 'DispatchLuis.cs')));

// End of arguments validation
// Initialize an instance of IDisconnectConfiguration to send the needed arguments to the disconnectSkill function
const configuration: IDisconnectConfiguration = {
    skillId: skillId,
    outFolder: outFolder,
    noRefresh: noRefresh,
    cognitiveModelsFile: cognitiveModelsFile,
    languages: languages,
    dispatchFolder: dispatchFolder,
    lgOutFolder: lgOutFolder,
    lgLanguage: lgLanguage,
    logger: logger,
    appSettingsFile: appSettingsFile
};
new DisconnectSkill(configuration as IDisconnectConfiguration, logger).disconnectSkill();
