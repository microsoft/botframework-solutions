/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { createHash } from 'crypto';

// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace MD5Util {
    export function computeHash(input: string): string {
        if (input === undefined || !input.trim()) {
            return '';
        }

        // Return the hexadecimal string.
        return createHash('md5')
            .update(input)
            .digest('hex');
    }
}
