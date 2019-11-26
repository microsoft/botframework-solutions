/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { gte } from 'semver';
import { ChildProcessUtils } from './childProcessUtils';

const azPreviewMessage: string = `Command group 'bot' is in preview. It may be changed/removed in a future release.\r\n`;

/**
 * @returns Returns if it is a preview message (az version greater than 2.0.66)
 */
export function isAzPreviewMessage(message: string): boolean {
    return message === azPreviewMessage;
}

/**
 * @returns Returns if it is a valid azure-cli version (lower than 2.0.66)
 */
const childProcess: ChildProcessUtils = new ChildProcessUtils();
// tslint:disable-next-line:export-name
export async function isValidAzVersion(): Promise<boolean> {
    const azVersionCommand: string[] = ['az', '--version'];
    const azVersion: string = await childProcess.tryExecute(azVersionCommand);
    const azVersionArr: string | undefined = azVersion.split('\r\n')
        .find((val: string): boolean => {
            return val.includes('azure-cli');
        });
    if (azVersionArr) {
        const azVersionNum: string = azVersionArr.split(' ')
            .filter((elem: string): string => elem)[1];

        return gte(azVersionNum, '2.0.67');
    }

    return false;
}
