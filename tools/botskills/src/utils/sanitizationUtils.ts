/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const regexTrailingBackslash = /.*?(\\|\/)+$/;

/**
 * @param path Path to sanitize.
 * @returns Returns a path which is sanitized
 */
export function sanitizePath(path: string): string {
    if (regexTrailingBackslash.test(path)) {
        return path.substring(0, path.length - 1);
    }

    return path;
}

/**
 * @param path Path to add quotes around in case it has spaces.
 * @returns Returns a path with quotes
 */
export function wrapPathWithQuotes(path: string): string {
    return `"${ path }"`;
}

