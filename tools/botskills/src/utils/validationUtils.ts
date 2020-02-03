/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ISkillManifestV1 } from '../models/manifestV1/skillManifestV1';
import { ISkillManifestV2 } from '../models/manifestV2/skillManifestV2';

/**
 * @param arg1 First argument of the pair of arguments.
 * @param arg2 Second argument of the pair of arguments.
 * @returns Returns an empty string if the validation is successful,
 * or a string with placeholders '{0}' and '{1}' for printing the necessary message.
 */
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

export function isInstanceOfISkillManifestV1(skillManifest: ISkillManifestV1): boolean {
    if (skillManifest.name === undefined ||
        skillManifest.id === undefined ||
        skillManifest.endpoint === undefined ||
        skillManifest.authenticationConnections === undefined ||
        skillManifest.actions === undefined ||
        skillManifest.actions[0] === undefined) {
        return false;
    }

    return true;
}

export function isInstanceOfISkillManifestV2(skillManifest: ISkillManifestV2): boolean {
    if (skillManifest.$schema === undefined ||
        skillManifest.$id === undefined ||
        skillManifest.endpoints === undefined ||
        skillManifest.dispatchModels === undefined ||
        skillManifest.activities === undefined || skillManifest.activities.size === 0) {
        return false;
    }

    return true;
}
