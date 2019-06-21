/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as dayjs from 'dayjs';
import i18next from 'i18next';

export namespace DateTimeExtensions {
    let currentLocale: string;

    function importLocale(locale: string): void {
        const localeImport: string = locale === 'zh' ?
        `../resources/customizeLocale/zh` :
        `dayjs/locale/${locale}`;
        import(localeImport);
    }

    export function toSpeechDateString(date: Date, includePrefix: boolean = false): string {
        const locale: string = i18next.language;
        if (currentLocale !== locale) {
            currentLocale = locale;
            importLocale(locale);
        }
        const utcDate: Date = new Date();
        utcDate.setHours(date.getHours());
        utcDate.setMinutes(date.getMinutes());
        utcDate.setSeconds(date.getSeconds());
        if (date.toUTCString() === utcDate.toUTCString()) {
            return i18next.t('common:today');
        }

        const nextDate: Date = new Date();
        nextDate.setDate(nextDate.getDate() + 1);
        nextDate.setHours(date.getHours());
        nextDate.setMinutes(date.getMinutes());
        nextDate.setSeconds(date.getSeconds());
        if (date.toUTCString() === nextDate.toUTCString()) {
            return i18next.t('common:tomorrow');
        }

        const prefix: string = i18next.t('common:spokenDatePrefix');
        if (includePrefix && prefix) {
            return `${prefix} ${dayjs(date)
                .locale(locale)
                .format(
                    i18next.t('common:spokenDateFormat')
                )}`;
        }

        return dayjs(date)
            .locale(locale)
            .format(
                i18next.t('common:spokenDateFormat')
            );
    }

    export function toSpeechTimeString(date: Date, includePrefix: boolean = false): string {
        if (includePrefix) {
            const prefix: string = date.getHours() === 1
                ? i18next.t('common:spokenTimePrefixOne')
                : i18next.t('common:spokenTimePrefixMoreThanOne');

            if (prefix) {
                return `${prefix} ${date.toLocaleTimeString()}`;
            }
        }

        return date.toLocaleTimeString();
    }
}
