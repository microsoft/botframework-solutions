/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

// tslint:disable: no-console
import chalk from 'chalk';

export interface ILogger {
    isError: boolean;
    isVerbose: boolean;

    error(message: string): void;
    message(message: string): void;
    success(message: string, withoutFormat?: boolean): void;
    warning(message: string): void;
}

export class ConsoleLogger implements ILogger {
    // tslint:disable: variable-name
    private _isError: boolean = false;
    private _isVerbose: boolean = false;
    // tslint:enable: variable-name
    public get isError(): boolean { return this._isError; }
    public get isVerbose(): boolean { return this._isVerbose; }
    public set isVerbose(value: boolean) { this._isVerbose = value || false; }

    public error(message: string): void {
        console.error(chalk.redBright(message));
        this._isError = true;
    }

    public message(message: string): void {
        if (this.isVerbose) {
            console.log(chalk.white(message));
        }
    }

    public success(message: string, withoutFormat: boolean = false): void {
        if (withoutFormat) {
            console.log(message);
        } else {
            console.log(chalk.greenBright(message));
        }
    }

    public warning(message: string): void {
        console.log(chalk.yellow(message));
    }
}
