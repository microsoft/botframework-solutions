/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import chalk from 'chalk';

export interface ILogger {
    isError: boolean;
    isVerbose: boolean;

    command(message: string, command: string): void;
    error(message: string): void;
    message(message: string): void;
    success(message: string, withoutFormat?: boolean): void;
    warning(message: string): void;
}

export class ConsoleLogger implements ILogger {
    private _isError: boolean = false;
    private _isVerbose: boolean = false;
    public get isError(): boolean { return this._isError; }
    public get isVerbose(): boolean { return this._isVerbose; }
    public set isVerbose(value: boolean) { this._isVerbose = value || false; }

    public error(message: string): void {
        console.error(chalk.redBright(message));
        this._isError = true;
    }

    public message(message: string): void {
        console.log(chalk.bold(message));
    }

    public success(message: string): void {
        console.log(chalk.greenBright(message));
    }

    public warning(message: string): void {
        console.log(chalk.yellow(message));
    }

    public command(message: string, command: string): void {
        if (this.isVerbose) {
            console.log(chalk.cyan(command));
        } else {
            this.message(message);
        }
    }
}
