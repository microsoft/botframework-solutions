/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
// tslint:disable:no-console
import * as chalk from 'chalk';
import * as program from 'commander';

program.Command.prototype.unknownOption = (flag: string): void => {
    console.error(chalk.default.redBright(`Unknown arguments: ${flag}`));
    showErrorHelp();
};

program
    .name('skill connect')
    .description('Connect the skill to your assistant bot')
    // .option('--serviceName <serviceName>', 'Azure Bot Service bot id')
    // .option('-n, --name <name>', 'Friendly name for this service (defaults to serviceName)')
    // .option('-t, --tenantId <tenantId>', 'id of the tenant for the Azure service (either GUID or xxx.onmicrosoft.com)')
    // .option('-s, --subscriptionId <subscriptionId>', 'GUID of the subscription for the Azure Service')
    // .option('-r, --resourceGroup <resourceGroup>', 'name of the resourceGroup for the Azure Service')
    // .option('-a, --appId  <appid>', 'Microsoft AppId for the Azure Bot Service\n')
    // .option('-e, --endpoint <endpoint>', 'Registered endpoint url for the Azure Bot Service')
    // .option('-p, --appPassword  <appPassword>', 'Microsoft AppPassword for the Azure Bot Service\n')
    // .option('-b, --bot <path>', 'path to bot file.  If omitted, local folder will look for a .bot file')
    // .option('--input <jsonfile>', 'path to arguments in JSON format { id:\'\',name:\'\', ... }')
    // .option('--secret <secret>', 'bot file secret password for encrypting service secrets')
    // .option('--stdin', 'arguments are passed in as JSON object via stdin')
    .option('-f', '--file', 'path to Skill Manifest')
    .action((cmd: program.Command, actions: program.Command) => undefined);

const args: program.Command = program.parse(process.argv);

if (process.argv.length < 3) {
    program.help();
} else {
    if (!args.file) {
        console.error(chalk.default.redBright('The "file" argument should be provided.'));
    } else {
        console.log(chalk.default.greenBright(`You provided the "file" argument which contains the following path: ${args.file}`));
    }
}

function showErrorHelp(): void {
    program.outputHelp((str: string) => {
        console.error(str);

        return '';
    });
    process.exit(1);
}
