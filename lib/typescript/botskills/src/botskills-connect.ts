/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { existsSync } from 'fs';
import { extname, isAbsolute, join, resolve } from 'path';
import { connectSkill } from './functionality';
import { ConsoleLogger, ILogger } from './logger/logger';
import { IConnectConfiguration } from './models';

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
    .name('botskills connect')
    .description('Connect a skill to your assistant bot')
    .option('-b, --botName <name>', 'Name of your assistant bot')
    .option('-l, --localManifest <path>', 'Path to local Skill Manifest file')
    .option('-r, --remoteManifest <url>', 'URL to remote Skill Manifest')
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

logger.isVerbose = args.verbose;

// Validation of arguments
// botName validation
if (!args.botName) {
    logger.error(`The 'botName' argument should be provided.`);
    process.exit(1);
}

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

// Initialize an instance of IConnectConfiguration to send the needed arguments to the connectSkill function
const configuration: Partial<IConnectConfiguration> = {
    botName: args.botName,
    localManifest: args.localManifest,
    remoteManifest: args.remoteManifest
};

// outFolder validation -- the const is needed for reassuring 'configuration.outFolder' is not undefined
const outFolder: string = args.outFolder || resolve('./');
configuration.outFolder = outFolder;

// skillsFile validation
if (!args.skillsFile) {
    configuration.skillsFile = join(configuration.outFolder, 'skills.json');
} else if (extname(args.skillsFile) !== '.json') {
    logger.error(`The 'skillsFile' argument should be a JSON file.`);
    process.exit(1);
} else {
    const skillsFilePath: string = isAbsolute(args.skillsFile) ? args.skillsFile : join(resolve('./'), args.skillsFile);
    if (!existsSync(skillsFilePath)) {
        logger.error(`The 'skillsFile' argument leads to a non-existing file.
            Please make sure to provide a valid path to your Assistant Skills configuration file.`);
        process.exit(1);
    }
    configuration.skillsFile = skillsFilePath;
}

// resourceGroup validation
configuration.resourceGroup = args.resourceGroup || configuration.botName;

// appSettingsFile validation
configuration.appSettingsFile = args.appSettingsFile || join(configuration.outFolder, 'appsettings.json');

// cognitiveModelsFile validation
const cognitiveModelsFilePath: string = args.cognitiveModelsFile || join(configuration.outFolder, 'cognitivemodels.json');
configuration.cognitiveModelsFile = cognitiveModelsFilePath;

// language validation
const language: string = args.language || 'en-us';
configuration.language = language;
const languageCode: string = (language.split('-'))[0];

// luisFolder validation
configuration.luisFolder = args.luisFolder || join(configuration.outFolder, 'Deployment', 'Resources', 'Skills', languageCode);

// dispatchFolder validation
configuration.dispatchFolder = args.dispatchFolder || join(configuration.outFolder, 'Deployment', 'Resources', 'Dispatch', languageCode);

// lgOutFolder validation
configuration.lgOutFolder = args.lgOutFolder || join(configuration.outFolder, 'Services');

// dispatchName validation
if (!args.dispatchName) {
    // try get the dispatch name from the cognitiveModels file
    // tslint:disable-next-line
    const cognitiveModelsFile: ICognitiveModelFile = require(cognitiveModelsFilePath);
    configuration.dispatchName = cognitiveModelsFile.cognitiveModels[languageCode].dispatchModel.name;
}

configuration.logger = logger;

// End of arguments validation

connectSkill(<IConnectConfiguration> configuration);

interface ICognitiveModelFile {
    cognitiveModels: {
        [key: string]: {
            dispatchModel: {
                name: string;
            };
        };
    };
}
