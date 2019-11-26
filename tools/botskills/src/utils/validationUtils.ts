/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

/**
 * @param arg1 First argument of the pair of arguments.
 * @param arg2 Second argument of the pair of arguments.
 * @returns Returns an empty string if the validation is successful,
 * or a string with placeholders '{0}' and '{1}' for printing the necessary message.
 */
// tslint:disable-next-line:export-name
export function validatePairOfArgs(arg1: string | undefined, arg2: string | undefined): string {
    if (!arg1 && !arg2) {
        return `One of the arguments '{0}' or '{1}' should be provided.`;
    } else if (arg1 && arg2) {
        return `Only one of the arguments '{0}' or '{1}' should be provided.`;
    }

    return '';
}

export function isValidCultures(availableCultures: string[], targetedCultures: string[]): boolean {
    if (availableCultures.length < 1) {
        return false;
    }
    const unavailableCulture: string[] = targetedCultures.reduce(
        (acc: string[], culture: string): string[] => {
            if (!availableCultures.includes(culture)) {
                acc.push(culture);
            }

            return acc;
        },
        []);

    if (unavailableCulture !== undefined && unavailableCulture.length > 0) {
        return false;
    }

    return true;
}
