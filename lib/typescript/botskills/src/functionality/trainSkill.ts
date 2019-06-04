/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { existsSync } from 'fs';
import { join } from 'path';
import { ConsoleLogger, ILogger } from '../logger';
import { ITrainConfiguration } from '../models';
import { ChildProcessUtils } from '../utils';

export class TrainSkill {
    public logger: ILogger;
    private childProcessUtils: ChildProcessUtils;
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

    private async updateDispatch(configuration: ITrainConfiguration): Promise<void> {
        try {
            const dispatchFile: string = `${configuration.dispatchName}.dispatch`;
            const dispatchJsonFile: string = `${configuration.dispatchName}.json`;
            const dispatchFilePath: string = join(configuration.dispatchFolder, dispatchFile);
            const dispatchJsonFilePath: string = join(configuration.dispatchFolder, dispatchJsonFile);
            this.logger.message('Running dispatch refresh...');
            const dispatchRefreshCommand: string[] = ['dispatch', 'refresh'];
            dispatchRefreshCommand.push(...['--dispatch', dispatchFilePath]);
            dispatchRefreshCommand.push(...['--dataFolder', configuration.dispatchFolder]);
            this.logger.message(await this.runCommand(
                dispatchRefreshCommand,
                `Executing dispatch refresh for the ${configuration.dispatchName} file`));
            if (!existsSync(dispatchJsonFilePath)) {
                // tslint:disable-next-line: max-line-length
                throw(new Error(`Path to ${dispatchJsonFile} (${dispatchJsonFilePath}) leads to a nonexistent file. Make sure the dispatch refresh command is being executed successfully`));
            }
        } catch (err) {
            this.logger.error(`There was an error in the dispatch refresh command:\n${err}`);
            throw err;
        }
    }

    private async runLuisGen(configuration: ITrainConfiguration): Promise<void> {
        try {
            const dispatchJsonFile: string = `${configuration.dispatchName}.json`;
            const dispatchJsonFilePath: string = join(configuration.dispatchFolder, dispatchJsonFile);
            this.logger.message('Running LuisGen...');
            const luisgenCommand: string[] = ['luisgen'];
            luisgenCommand.push(dispatchJsonFilePath);
            luisgenCommand.push(...[`-${configuration.lgLanguage}`, `"DispatchLuis"`]);
            luisgenCommand.push(...['-o', configuration.lgOutFolder]);
            await this.runCommand(luisgenCommand, `Executing luisgen for the ${configuration.dispatchName} file`);
        } catch (err) {
            this.logger.error(`There was an error in the luisgen command:\n${err}`);
            throw err;
        }
    }

    public async trainSkill(configuration: ITrainConfiguration): Promise<boolean> {
        try {
            await this.updateDispatch(configuration);
            await this.runLuisGen(configuration);
            this.logger.success('Successfully trained Dispatch model');

            return true;
        } catch (err) {
            this.logger.error(`There was an error while training any Skill from the Assistant:\n${err}`);

            return false;
        }
    }
}
