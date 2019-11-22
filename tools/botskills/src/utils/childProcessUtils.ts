/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as childProcess from 'child_process';
import { join } from 'path';
import { isAzPreviewMessage } from './';

export class ChildProcessUtils {

    private async execDispatch(args: string[]): Promise<string> {
        const dispatchPath: string = join(__dirname, '..', '..', 'node_modules', 'botdispatch', 'bin', 'netcoreapp2.1', 'Dispatch.dll');

        // tslint:disable-next-line: typedef
        return new Promise((pResolve, pReject): void => {
            childProcess.spawn('dotnet', [dispatchPath, ...args], { stdio: 'inherit' })
                .on('close', (code: number): void => {
                    pResolve('');
                })
                .on('error', (err: Error): void => {
                    pReject(err);
                });
        });
    }

    public async execute(command: string, args: string[]): Promise<string> {
        if (command === 'dispatch') {
            return this.execDispatch(args);
        }

        // tslint:disable-next-line: typedef
        return new Promise((pResolve, pReject): void => {
            childProcess.exec(
                `${command} ${args.join(' ')}`,
                (err: childProcess.ExecException | null, stdout: string, stderr: string): void => {
                    if (stderr && !stderr.includes('Update available')) {
                        pReject(stderr);
                    }
                    pResolve(stdout);
                });
        });
    }

    public async tryExecute(command: string[]): Promise<string> {
        // tslint:disable-next-line: typedef
        return new Promise((pResolve, pReject): void => {
            try {
                childProcess.exec(
                    `${command.join(' ')}`,
                    (err: childProcess.ExecException | null, stdout: string, stderr: string): void => {
                        if (stderr && !isAzPreviewMessage(stderr)) {
                            pReject(stderr);
                        }
                        pResolve(stdout);
                    });
            } catch (err) {
                pReject(err);
            }
        });
    }
}
