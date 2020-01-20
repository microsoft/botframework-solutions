/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

const regexTrailingBackslash: RegExp = /.*?(\\)+$/;

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
    return `"${path}"`;
}

/**
 * @param endpoint URL of the remote manifest endpoint.
 * @param isInlineUtterances Value of the --inlineUtterances parameter.
 * @returns Returns an endpoint based on the --inlineUtterances parameter.
 */
export function sanitizeInlineUtterancesEndpoint(endpoint: string, isInlineUtterances: boolean): string {
    let url: string = endpoint.split('?')[0];
    const paramName: string = 'inlineTriggerUtterances';
    const urlParams: URLSearchParams = new URL(endpoint).searchParams;
    const hasParam: boolean = urlParams.has(paramName);
  
    if (isInlineUtterances) {
        url += '?';
        if (hasParam) {
            urlParams.set(paramName, 'true');
        } else {
            urlParams.append(paramName, 'true');
        }
    } else if (hasParam) {
        urlParams.delete(paramName);
    }
  
    return url + urlParams.toString();
}
