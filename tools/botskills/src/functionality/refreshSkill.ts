/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { existsSync } from 'fs';
import { join } from 'path';
import { ConsoleLogger, ILogger } from '../logger';
import { IRefreshConfiguration } from '../models';
import { ChildProcessUtils } from '../utils';

export class RefreshSkill {
    public logger: ILogger;
    private childProcessUtils: ChildProcessUtils;
    private dispatchFile: string = '';
    private dispatchJsonFile: string = '';
    private dispatchFilePath: string = '';
    private dispatchJsonFilePath: string = '';

    constructor(logger?: ILogger) {
        this.logger = logger || new ConsoleLogger();
        this.childProcessUtils = new ChildProcessUtils();
    }

    private async runCommand(command: string[], description: string): Promise<string> {
        this.logger.command(description, command.join(' '));
        const cmd: string = command[0];
        const commandArgs: string[] = command.slice(1)
            .filter((arg: string) => arg);
        try {
            return await this.childProcessUtils.execute(cmd, commandArgs);
        } catch (err) {
            throw err;
        }
    }

    private async updateDispatch(configuration: IRefreshConfiguration): Promise<void> {
        const dispatchRefreshCommand: string[] = ['dispatch', 'refresh'];
        try {
            this.logger.message('Running dispatch refresh...');

            dispatchRefreshCommand.push(...['--dispatch', this.dispatchFilePath]);
            dispatchRefreshCommand.push(...['--dataFolder', configuration.dispatchFolder]);

            await this.runCommand(
                dispatchRefreshCommand,
                `Executing dispatch refresh for the ${configuration.dispatchName} file`);

            if (!existsSync(this.dispatchJsonFilePath)) {
                // tslint:disable-next-line: max-line-length
                throw new Error(`Path to ${this.dispatchJsonFile} (${this.dispatchJsonFilePath}) leads to a nonexistent file. This may be due to a problem with the 'dispatch refresh' command.`);
            }
        } catch (err) {
            throw new Error(`There was an error in the dispatch refresh command:\nCommand:${dispatchRefreshCommand}\n${err}`);
        }
    }

    private async runLuisGen(configuration: IRefreshConfiguration): Promise<void> {
        const luisgenCommand: string[] = ['luisgen'];
        try {
            this.logger.message('Running LuisGen...');

            luisgenCommand.push(this.dispatchJsonFilePath);
            luisgenCommand.push(...[`-${configuration.lgLanguage}`, `"DispatchLuis"`]);
            luisgenCommand.push(...['-o', configuration.lgOutFolder]);

            await this.runCommand(luisgenCommand, `Executing luisgen for the ${configuration.dispatchName} file`);
        } catch (err) {
            throw new Error(`There was an error in the luisgen command:\nCommand: ${luisgenCommand.join(' ')}\n${err}`);
        }
    }

    public async refreshSkill(configuration: IRefreshConfiguration): Promise<boolean> {
        try {
            this.dispatchFile = `${configuration.dispatchName}.dispatch`;
            this.dispatchJsonFile = `${configuration.dispatchName}.json`;
            this.dispatchFilePath = join(configuration.dispatchFolder, this.dispatchFile);
            this.dispatchJsonFilePath = join(configuration.dispatchFolder, this.dispatchJsonFile);

            if (!existsSync(configuration.dispatchFolder)) {
                throw new Error(`Path to the Dispatch folder (${configuration.dispatchFolder}) leads to a nonexistent folder.
Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
            } else if (!existsSync(this.dispatchFilePath)) {
                throw new Error(`Path to the ${this.dispatchFile} file leads to a nonexistent file.
Make sure to use the argument '--dispatchName' for your Assistant's Dispatch file name.`);
            }

            await this.updateDispatch(configuration);
            await this.runLuisGen(configuration);
            this.logger.success('Successfully refreshed Dispatch model');
            this.logger.warning(
                'You need to re-publish your Virtual Assistant in order to have these changes available for Azure based testing');

            return true;
        } catch (err) {
            this.logger.error(`There was an error while refreshing any Skill from the Assistant:\n${err}`);

            return false;
        }
    }
}
