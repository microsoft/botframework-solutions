/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { existsSync, readFileSync } from 'fs';
import { join } from 'path';
import { ConsoleLogger, ILogger } from '../logger';
import { ICognitiveModelFile, IRefreshConfiguration } from '../models';
import { ChildProcessUtils, getDispatchNames, wrapPathWithQuotes } from '../utils';

export class RefreshSkill {
    public logger: ILogger;
    private readonly childProcessUtils: ChildProcessUtils;
    private readonly configuration: IRefreshConfiguration;

    public constructor(configuration: IRefreshConfiguration, logger?: ILogger) {
        this.configuration = configuration;
        this.logger = logger || new ConsoleLogger();
        this.childProcessUtils = new ChildProcessUtils();
    }

    private async runCommand(command: string[], description: string): Promise<string> {
        this.logger.command(description, command.join(' '));
        const cmd: string = command[0];
        const commandArgs: string[] = command.slice(1)
            .filter((arg: string): string => arg);
        try {
            return await this.childProcessUtils.execute(cmd, commandArgs);
        } catch (err) {
            throw err;
        }
    }

    private async executeDispatchRefresh(dispatchName: string, executionModelByCulture: Map<string, string>): Promise<void> {
        const dispatchRefreshCommand: string[] = ['dispatch', 'refresh'];
        try {
            this.logger.message(`Running dispatch refresh for ${dispatchName}...`);
            // tslint:disable:no-backbone-get-set-outside-model
            const dispatchJsonFile: string = <string> executionModelByCulture.get('dispatchJsonFile');
            const dispatchJsonFilePath: string = <string> executionModelByCulture.get('dispatchJsonFilePath');
            // tslint:enable:no-backbone-get-set-outside-model
            const dispatchRefreshCommandArguments: string[] = ['--dispatch', '--dataFolder'];
            dispatchRefreshCommandArguments.forEach((argument: string): void => {
                const argumentValue: string = <string> executionModelByCulture.get(argument);
                dispatchRefreshCommand.push(...[argument, argumentValue]);
            });
            await this.runCommand(
                dispatchRefreshCommand,
                `Executing dispatch refresh for the ${dispatchName} file`);

            if (!existsSync(dispatchJsonFilePath)) {
                // tslint:disable-next-line: max-line-length
                throw new Error(`Path to ${dispatchJsonFile} (${dispatchJsonFilePath}) leads to a nonexistent file. This may be due to a problem with the 'dispatch refresh' command.`);
            }
        } catch (err) {
            throw new Error(`There was an error in the dispatch refresh command:\nCommand: ${dispatchRefreshCommand.join(' ')}\n${err}`);
        }
    }

    private async executeLuisGen(dispatchName: string, executionModelByCulture: Map<string, string>): Promise<void> {
        const luisgenCommand: string[] = ['luisgen'];
        try {
            this.logger.message(`Running LuisGen for ${dispatchName}...`);
            const dispatchJsonFilePath: string = <string> executionModelByCulture.get('dispatchJsonFilePath');
            const luisgenCommandArguments: string [] = [
                wrapPathWithQuotes(dispatchJsonFilePath),
                `-${this.configuration.lgLanguage}`,
                '-o'
            ];
            luisgenCommandArguments.forEach((argument: string): void => {
                const argumentValue: string = <string> executionModelByCulture.get(argument);
                luisgenCommand.push(...[argument, argumentValue]);
            });
            this.logger.message(luisgenCommand.join(' '));
            await this.runCommand(luisgenCommand, `Executing luisgen for the ${dispatchName} file`);
        } catch (err) {
            throw new Error(`There was an error in the luisgen command:\nCommand: ${luisgenCommand.join(' ')}\n${err}`);
        }
    }

    private getExecutionModel(culture: string, dispatchName: string): Map<string, string> {
        const dispatchFile: string = `${dispatchName}.dispatch`;
        const dispatchJsonFile: string = `${dispatchName}.json`;
        const dispatchFilePath: string = join(this.configuration.dispatchFolder, culture, dispatchFile);
        const dispatchJsonFilePath: string = join(this.configuration.dispatchFolder, culture, dispatchJsonFile);
        const dataFolder: string = join(this.configuration.dispatchFolder, culture);
        if (!existsSync(this.configuration.dispatchFolder)) {
            throw new Error(`Path to the Dispatch folder (${this.configuration.dispatchFolder}) leads to a nonexistent folder.
Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        } else if (!existsSync(dispatchFilePath)) {
            throw new Error(`Path to the ${dispatchFile} file leads to a nonexistent file.`);
        }

        // tslint:disable:no-backbone-get-set-outside-model
        const executionModelMap: Map<string, string> = new Map();
        executionModelMap.set('dispatchJsonFile', dispatchJsonFile);
        executionModelMap.set('dispatchJsonFilePath', dispatchJsonFilePath);
        executionModelMap.set('--dispatch', dispatchFilePath);
        executionModelMap.set('--dataFolder', dataFolder);
        executionModelMap.set(wrapPathWithQuotes(dispatchJsonFilePath), '');
        executionModelMap.set(`-${this.configuration.lgLanguage}`, wrapPathWithQuotes('DispatchLuis'));
        executionModelMap.set('-o', wrapPathWithQuotes(this.configuration.lgOutFolder));
        // tslint:enable:no-backbone-get-set-outside-model

        return executionModelMap;
    }

    private async updateModel(): Promise<void> {
        if (!existsSync(this.configuration.cognitiveModelsFile)) {
            throw new Error(`Could not find the cognitiveModels file (${
                this.configuration.cognitiveModelsFile}). Please provide the '--cognitiveModelsFile' argument.`);
        }
        // eslint-disable-next-line @typescript-eslint/tslint/config
        const cognitiveModelsFile: ICognitiveModelFile = JSON.parse(readFileSync(this.configuration.cognitiveModelsFile, 'UTF8'));
        const dispatchNames: Map<string, string> = getDispatchNames(cognitiveModelsFile);
        const executionsModelMap: Map<string, Map<string, string>> = new Map();
        dispatchNames.forEach((dispatchName: string, culture: string): void => {
            executionsModelMap.set(culture, this.getExecutionModel(culture, dispatchName));
        });

        await Promise.all(Array.from(executionsModelMap.entries())
            .map(async (item: [string, Map<string, string>]): Promise<void> => {
                const culture: string = item[0];
                const executionModelByCulture: Map<string, string> = item[1];
                const dispatchName: string = <string> dispatchNames.get(culture);
                await this.executeDispatchRefresh(dispatchName, executionModelByCulture);
                await this.executeLuisGen(dispatchName, executionModelByCulture);
            }));
    }

    public async refreshSkill(): Promise<boolean> {
        try {
            await this.updateModel();
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
