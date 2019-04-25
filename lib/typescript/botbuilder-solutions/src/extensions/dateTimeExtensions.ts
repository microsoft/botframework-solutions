/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import * as dateFormat from 'dateformat';
import i18next from 'i18next';

export namespace DateTimeExtensions {
    export function toSpeechDateString(date: Date, includePrefix: boolean = false): string {
        const utcDate: Date = new Date(date.toUTCString());

        if (date.toDateString() === utcDate.toDateString()) {
            return i18next.t('common:today');
        }

        const nextDate: Date = new Date(date.toString());
        nextDate.setDate(date.getDate() + 1);

        if (nextDate.toDateString() === utcDate.toDateString()) {
            return i18next.t('common:tomorrow');
        }

        const prefix: string = i18next.t('common:spokenDatePrefix');
        if (includePrefix && prefix) {
            return `${prefix} ${dateFormat(date, i18next.t('common:spokenDateFormat'))}`;
        }

        return dateFormat(date, i18next.t('common:spokenDateFormat'));
    }

    export function toSpeechTimeString(date: Date, includePrefix: boolean = false): string {
        if (includePrefix) {
            const prefix: string = date.getHours() === 1
                ? i18next.t('common:spokenTimePrefix_One')
                : i18next.t('common:spokenTimePrefix_MoreThanOne');

            if (prefix) {
                return `${prefix} ${date.toTimeString()}`;
            }
        }

        return date.toTimeString();
    }
}
