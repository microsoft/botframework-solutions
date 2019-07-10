/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const regexTrailingBackslash: RegExp = /.*?(\\)+$/;

/**
 * @param path Path to sanitize.
 * @returns Returns a path which is sanitized
 */
// tslint:disable-next-line:export-name
export function sanitizePath(path: string): string {
    if (regexTrailingBackslash.test(path)) {
        return path.substring(0, path.length - 1);
    }

    return path;
}
