/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import i18next from 'i18next';

export namespace ListExtensions {
    /**
     * Converts a list into a string that can be used in speech.
     * @param list The list to be converted.
     * @param finalSeparator The separator to be used for the last element of the list ("and" or "or" for example).
     * @param stringAccessor A method that can be used to extract the elements from the list if it is a complex type.
     * @returns A comma separated string with the elements in the list.
     */
    export function toSpeechString<T>(list: T[], finalSeparator: string, stringAccessor?: (value: T) => string): string {
        // If stringAccessor is undefined, use JSON.stringify to convert the T value
        const itemAccessor: (value: T) => string = stringAccessor || JSON.stringify;

        let speech: string = '';
        let separator: string = '';

        const listCount: number = list.length;
        list.forEach((listItem: T, index: number) => {
            speech = speech + itemAccessor(listItem);
            if (listCount > 1) {
                if (index === listCount - 2) {
                    separator = i18next.t('common:separatorFormat', finalSeparator);
                } else {
                    separator = index !== listCount - 1 ? ', ' : '';
                }

                speech = speech + separator;
            }
        });

        return speech;
    }
}
