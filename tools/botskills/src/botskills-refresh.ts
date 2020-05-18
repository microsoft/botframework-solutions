/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as program from 'commander';
import { join, resolve } from 'path';
import { RefreshSkill } from './functionality';
import { ConsoleLogger, ILogger} from './logger';
import { IRefreshConfiguration } from './models';
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
    .name('botskills refresh')
    .description('Refresh the connected skills.')
    .option('--cs', 'Determine your assistant project structure to be a CSharp-like structure')
    .option('--ts', 'Determine your assistant project structure to be a TypeScript-like structure')
    .option('--dispatchFolder [path]', '[OPTIONAL] Path to the folder containing your assistant\'s \'.dispatch\' file (defaults to \'./deployment/resources/dispatch\' inside your assistant folder)')
    .option('--outFolder [path]', '[OPTIONAL] Path for any output file that may be generated (defaults to your assistant\'s root folder)')
    .option('--lgOutFolder [path]', '[OPTIONAL] Path for the Luis Generate output (defaults to a \'service\' folder inside your assistant\'s folder)')
    .option('--cognitiveModelsFile [path]', '[OPTIONAL] Path to your Cognitive Models file (defaults to \'cognitivemodels.json\' inside your assistant\'s folder)')
    .option('--verbose', '[OPTIONAL] Output detailed information about the processing of the tool')
    .action((cmd: program.Command, actions: program.Command): undefined => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
    process.exit(0);
}

let dispatchFolder: string;
let lgLanguage: string;
let outFolder: string;
let lgOutFolder: string;
let cognitiveModelsFile: string;

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

// outFolder validation -- the const is needed for reassuring 'configuration.outFolder' is not undefined
outFolder = args.outFolder ? sanitizePath(args.outFolder) : resolve('./');

// cognitiveModelsFile validation
cognitiveModelsFile = args.cognitiveModelsFile || join(outFolder, (args.ts ? join('src', 'cognitivemodels.json') : 'cognitivemodels.json'));

// dispatchFolder validation
dispatchFolder = args.dispatchFolder ? sanitizePath(args.dispatchFolder) : join(outFolder, 'Deployment', 'Resources', 'Dispatch');

// lgOutFolder validation
lgOutFolder = args.lgOutFolder ? sanitizePath(args.lgOutFolder) : join(outFolder, (args.ts ? join('src', 'Services', 'DispatchLuis.ts') : join('Services', 'DispatchLuis.cs')));

// End of arguments validation

// Initialize an instance of IRefreshConfiguration to send the needed arguments to the refreshskill function
const configuration: IRefreshConfiguration = {
    dispatchFolder: dispatchFolder,
    lgLanguage: lgLanguage,
    outFolder: outFolder,
    lgOutFolder: lgOutFolder,
    cognitiveModelsFile: cognitiveModelsFile,
    logger: logger
};

new RefreshSkill(configuration as IRefreshConfiguration, logger).refreshSkill();
