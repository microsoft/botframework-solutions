/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { existsSync, readFileSync } from 'fs';
import { join } from 'path';
import { ConsoleLogger, ILogger } from '../logger';
import { ICognitiveModel, IRefreshConfiguration } from '../models';
import { ChildProcessUtils, getDispatchNames, isCloudGovernment, libraries, validateLibrary, wrapPathWithQuotes } from '../utils';
import { EOL } from 'os';

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
            this.logger.message(`Running dispatch refresh for ${ dispatchName }...`);
            const dispatchJsonFile: string = executionModelByCulture.get('dispatchJsonFile') as string;
            const dispatchJsonFilePath: string = executionModelByCulture.get('dispatchJsonFilePath') as string;
            const dispatchRefreshCommandArguments: string[] = ['--dispatch', '--dataFolder'];
            dispatchRefreshCommandArguments.forEach((argument: string): void => {
                const argumentValue: string = executionModelByCulture.get(argument) as string;
                dispatchRefreshCommand.push(...[argument, argumentValue]);
            });
            // Append '--gov true' if it is a government cloud
            if (await isCloudGovernment() === true) {
                dispatchRefreshCommand.push('--gov', 'true');
            }
            await this.runCommand(
                dispatchRefreshCommand,
                `Executing dispatch refresh for the ${ dispatchName } file`);

            if (!existsSync(dispatchJsonFilePath)) {
                throw new Error(`Path to ${ dispatchJsonFile } (${ dispatchJsonFilePath }) leads to a nonexistent file. This may be due to a problem with the 'dispatch refresh' command.`);
            }
        } catch (err) {
            throw new Error(`There was an error in the dispatch refresh command:${ EOL }Command: ${ dispatchRefreshCommand.join(' ') + EOL + err }`);
        }
    }

    private async executeLuisGenerate(dispatchName: string, executionModelByCulture: Map<string, string>): Promise<void> {
        const luisGenerateCommand: string[] = ['bf', `luis:generate:${ this.configuration.lgLanguage }`];
        try {
            this.logger.message(`Running Luis Generate for ${ dispatchName }...`);
            const luisGenerateCommandArguments: string [] = [
                '--in',
                '--out',
                '--className'
            ];
            luisGenerateCommandArguments.forEach((argument: string): void => {
                const argumentValue: string = executionModelByCulture.get(argument) as string;
                luisGenerateCommand.push(...[argument, wrapPathWithQuotes(argumentValue)]);
            });

            // Force the bf luis:generate to overwrite the output file if it already exists
            luisGenerateCommand.push('--force');
            await this.runCommand(luisGenerateCommand, `Executing luisgen for the ${ dispatchName } file`);
        } catch (err) {
            throw new Error(`There was an error in the bf luis:generate:${ this.configuration.lgLanguage } command:${ EOL }Command: ${ luisGenerateCommand.join(' ') + EOL + err }`);
        }
    }
    
    private getExecutionModel(culture: string, dispatchName: string): Map<string, string> {
        const dispatchFile = `${ dispatchName }.dispatch`;
        const dispatchJsonFile = `${ dispatchName }.json`;
        const dispatchFilePath: string = join(this.configuration.dispatchFolder, culture, dispatchFile);
        const dispatchJsonFilePath: string = join(this.configuration.dispatchFolder, culture, dispatchJsonFile);
        const dataFolder: string = join(this.configuration.dispatchFolder, culture);
        if (!existsSync(this.configuration.dispatchFolder)) {
            throw new Error(`Path to the Dispatch folder (${ this.configuration.dispatchFolder }) leads to a nonexistent folder.${
                EOL }Remember to use the argument '--dispatchFolder' for your Assistant's Dispatch folder.`);
        } else if (!existsSync(dispatchFilePath)) {
            throw new Error(`Path to the ${ dispatchFile } file leads to a nonexistent file.`);
        }

        const executionModelMap: Map<string, string> = new Map();
        executionModelMap.set('dispatchJsonFile', dispatchJsonFile);
        executionModelMap.set('dispatchJsonFilePath', dispatchJsonFilePath);
        executionModelMap.set('--dispatch', dispatchFilePath);
        executionModelMap.set('--dataFolder', dataFolder);
        executionModelMap.set('--in', dispatchJsonFilePath);
        executionModelMap.set('--out', this.configuration.lgOutFolder);
        executionModelMap.set('--className', 'DispatchLuis');
        
        return executionModelMap;
    }

    private async updateModel(): Promise<void> {
        if (!existsSync(this.configuration.cognitiveModelsFile)) {
            throw new Error(`Could not find the cognitiveModels file (${
                this.configuration.cognitiveModelsFile }). Please provide the '--cognitiveModelsFile' argument.`);
        }
        
        const cognitiveModelsFile: ICognitiveModel = JSON.parse(readFileSync(this.configuration.cognitiveModelsFile, 'UTF8'));
        const dispatchNames: Map<string, string> = getDispatchNames(cognitiveModelsFile);
        const executionsModelMap: Map<string, Map<string, string>> = new Map();
        dispatchNames.forEach((dispatchName: string, culture: string): void => {
            executionsModelMap.set(culture, this.getExecutionModel(culture, dispatchName));
        });

        await Promise.all(Array.from(executionsModelMap.entries())
            .map(async (item: [string, Map<string, string>]): Promise<void> => {
                const culture: string = item[0];
                const executionModelByCulture: Map<string, string> = item[1];
                const dispatchName: string = dispatchNames.get(culture) as string;
                await this.executeDispatchRefresh(dispatchName, executionModelByCulture);
                await this.executeLuisGenerate(dispatchName, executionModelByCulture);
            }));
    }

    public async refreshSkill(): Promise<boolean> {
        try {
            // Validate if the user has the necessary tools to run the command
            await validateLibrary([libraries.BotFrameworkCLI], this.logger);
            if (this.logger.isError) {
                throw new Error('You have not installed the required tools to run this command');
            }
            
            await this.updateModel();
                
            this.logger.success('Successfully refreshed Dispatch model');
            this.logger.warning(
                'You need to re-publish your Virtual Assistant in order to have these changes available for Azure based testing');

            return true;
        } catch (err) {
            this.logger.error(`There was an error while refreshing any Skill from the Assistant:${ EOL + err }`);

            return false;
        }
    }
}
