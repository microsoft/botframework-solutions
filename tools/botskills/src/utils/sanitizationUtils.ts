/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { readFileSync } from 'fs';
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
 * @param appsettings Path to the appsettings file.
 * @returns Returns an appsettings which is sanitized
 */
export function sanitizeAppSettingsProperties(appsettings: string): string {
    return readFileSync(appsettings, 'UTF8')
        .replace(new RegExp('\\bBotFrameworkSkills\\b', 'gi'), 'botFrameworkSkills')
        .replace(new RegExp('\\bAppId\\b', 'gi'), 'appId')
        .replace(new RegExp('\\bId\\b', 'gi'), 'id')
        .replace(new RegExp('\\bSkillEndpoint\\b', 'gi'), 'skillEndpoint')
        .replace(new RegExp('\\bName\\b', 'gi'), 'name')
        .replace(new RegExp('\\bDescription\\b', 'gi'), 'description')
        .replace(new RegExp('\\bSkillHostEndpoint\\b', 'gi'), 'skillHostEndpoint');    
}

/**
 * @param path Path to add quotes around in case it has spaces.
 * @returns Returns a path with quotes
 */
export function wrapPathWithQuotes(path: string): string {
    return `"${ path }"`;
}
