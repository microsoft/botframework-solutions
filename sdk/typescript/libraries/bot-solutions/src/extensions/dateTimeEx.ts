/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as dayjs from 'dayjs';
import { readFileSync } from 'fs';
import { CommonResponses } from '../resources';
import { ResponsesUtil } from '../util/responsesUtil';

// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace DateTimeEx {
    let currentLocale: string;
    async function importLocale(locale: string): Promise<void> {
        try {
            const localeImport: string = locale === 'zh' ?
                `../resources/customizeLocale/zh` :
                `dayjs/locale/${ locale }`;
            await import(localeImport);
        } catch (err) {
            throw new Error(`There was an error during the import of the locale:${ locale }, Error:${ err }`);
        }
    }

    export async function toSpeechDateString(date: Date, locale: string, includePrefix: boolean = false): Promise<string> {
        if (currentLocale !== locale) {
            currentLocale = locale;
            await importLocale(locale);
        }
        const utcDate: Date = new Date();
        utcDate.setHours(date.getHours());
        utcDate.setMinutes(date.getMinutes());
        utcDate.setSeconds(date.getSeconds());
        const jsonPath = ResponsesUtil.getResourcePath(CommonResponses.name, CommonResponses.pathToResource, locale);
        const commonFile: string = readFileSync(jsonPath, 'utf8');
        if (date.toUTCString() === utcDate.toUTCString()) {
            return JSON.parse(commonFile)['today'];
        }

        const nextDate: Date = new Date();
        nextDate.setDate(nextDate.getDate() + 1);
        nextDate.setHours(date.getHours());
        nextDate.setMinutes(date.getMinutes());
        nextDate.setSeconds(date.getSeconds());
        if (date.toUTCString() === nextDate.toUTCString()) {
            return JSON.parse(commonFile)['tomorrow'];
        }

        const prefix: string = JSON.parse(commonFile)['spokenDatePrefix'];
        if (includePrefix && prefix) {
            return `${ prefix } ${ dayjs(date)
                .locale(locale)
                .format(
                    JSON.parse(commonFile)['spokenDateFormat']
                ) }`;
        }

        return dayjs(date)
            .locale(locale)
            .format(
                JSON.parse(commonFile)['spokenDateFormat']
            );
    }

    export function toSpeechTimeString(date: Date, locale: string, includePrefix: boolean = false): string {
        if (includePrefix) {
            const jsonPath: string = ResponsesUtil.getResourcePath(CommonResponses.name, CommonResponses.pathToResource, locale);
            const commonFile: string = readFileSync(jsonPath, 'utf8');
            const prefix: string = date.getHours() === 1
                ? JSON.parse(commonFile)['spokenTimePrefixOne']
                : JSON.parse(commonFile)['spokenTimePrefixMoreThanOne'];

            if (prefix) {
                return `${ prefix } ${ date.toLocaleTimeString() }`;
            }
        }

        return date.toLocaleTimeString();
    }
}
