/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as child_process from 'child_process';
import { join } from 'path';

async function execDispatch(args: string[]): Promise<string> {
    const dispatchPath: string = join(__dirname, '..', '..', 'node_modules', 'botdispatch', 'bin', 'netcoreapp2.1', 'Dispatch.dll');

    // tslint:disable-next-line: typedef
    return new Promise((pResolve, pReject) => {
        child_process.spawn('dotnet', [dispatchPath, ...args], { stdio: 'inherit' })
            .on('close', (code: number) => {
                pResolve('');
            })
            .on('error', (err: Error) => {
                pReject(err);
            });
    });
}

export async function spawn(command: string, args: string[]): Promise<string> {

    // tslint:disable-next-line: typedef
    return new Promise((pResolve, pReject) => {
        child_process.spawn(command, args, { stdio: 'inherit', env: process.env, argv0: command, cwd: join(__dirname, '..') })
            .on('close', (code: number) => {
                pResolve('');
            })
            .on('error', (err: Error) => {
                pReject(err);
            });
    });
}

export async function execute(command: string, args: string[]): Promise<string> {
    if (command === 'dispatch') {
        return execDispatch(args);
    }

    // tslint:disable-next-line: typedef
    return new Promise((pResolve, pReject) => {
        child_process.exec(`${command} ${args.join(' ')}`, (err: child_process.ExecException | null, stdout: string, stderr: string) => {
            if (stderr) {
                pReject(stderr);
            }
            pResolve(stdout);
        });
    });
}

export async function tryExecute(command: string, args: string[]): Promise<string> {
    // tslint:disable-next-line: typedef
    return new Promise((pResolve, pReject) => {
        try {
            child_process.exec(
                `${command} ${args.join(' ')}`,
                (err: child_process.ExecException | null, stdout: string, stderr: string) => {
                    if (stderr) {
                        pReject(stderr);
                    }
                    pResolve(stdout);
            });
        } catch (err) {

            return err;
        }
    });
}

export function extractArgs(command: string): string[] {
    const parts: string[] = command.split(' ');

    return parts.slice(1)
        .filter((arg: string) => arg);
}
