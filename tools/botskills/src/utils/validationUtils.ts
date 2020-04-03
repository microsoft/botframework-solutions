/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ISkillManifestV1 } from '../models/manifestV1/skillManifestV1';
import { ISkillManifestV2 } from '../models/manifestV2/skillManifestV2';
import { ILogger } from '../logger';

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

export function manifestV1Validation(skillManifest: ISkillManifestV1, logger: ILogger): void {
    if (!skillManifest.name) {
        logger.error(`Missing property 'name' of the manifest`);
    }
    if (!skillManifest.id) {
        logger.error(`Missing property 'id' of the manifest`);
    } else if (skillManifest.id.match(/^\d|[^\w]/g) !== null) {
        logger.error(`The 'id' of the manifest contains some characters not allowed. Make sure the 'id' contains only letters, numbers and underscores, but doesn't start with number.`);
    }
    if (!skillManifest.endpoint) {
        logger.error(`Missing property 'endpoint' of the manifest`);
    } else if (skillManifest.endpoint.match(/^(https?:\/\/)?((([a-zA-F\d]([a-zA-F\d-]*[a-zA-F\d])*)\.)+[a-zA-F]{2,}|(((\d{1,3}\.){3}\d{1,3})|(localhost))(\:\d+)?)(\/[-a-zA-F\d%_.~+]*)*(\?[;&a-zA-F\d%_.~+=-]*)?(\#[-a-zA-F\d_]*)?$/g) === null) {
        logger.error(`The 'endpoint' property contains some characters not allowed.`);
    }
    if (skillManifest.authenticationConnections === undefined || !skillManifest.authenticationConnections) {
        logger.error(`Missing property 'authenticationConnections' of the manifest`);
    }
    if (!skillManifest.actions || skillManifest.actions === undefined || skillManifest.actions[0] === undefined) {
        logger.error(`Missing property 'actions' of the manifest`);
    }

}

export function manifestV2Validation(skillManifest: ISkillManifestV2, logger: ILogger, endpointName?: string): void {
    if (!skillManifest.$schema) {
        logger.error(`Missing property '$schema' of the manifest`);
    }
    if (!skillManifest.$id) {
        logger.error(`Missing property '$id' of the manifest`);
    } else if (skillManifest.$id.match(/^\d|[^\w]/g) !== null) {
        logger.error(`The '$id' of the manifest contains some characters not allowed. Make sure the '$id' contains only letters, numbers and underscores, but doesn't start with number.`);
    }
    if (!skillManifest.endpoints) {
        logger.error(`Missing property 'endpoints' of the manifest`);
    }

    let currentEndpoint = skillManifest.endpoints.find((endpoint): boolean =>  endpoint.name == endpointName) || skillManifest.endpoints[0];
    if (!currentEndpoint.name){
        logger.error(`Missing property 'name' at the selected endpoint. If you didn't select any endpoint, the first one is taken by default`);
    }

    if (!currentEndpoint.msAppId){
        logger.error(`Missing property 'msAppId' at the selected endpoint. If you didn't select any endpoint, the first one is taken by default`);
    } else if (currentEndpoint.msAppId.match(/^[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}$/g) === null) {
        logger.error(`The 'msAppId' property contains some characters not allowed at the selected endpoint. If you didn't select any endpoint, the first one is taken by default.`);
    }

    if (!currentEndpoint.endpointUrl){
        logger.error(`Missing property 'endpointUrl' at the selected endpoint. If you didn't select any endpoint, the first one is taken by default`);
    } else if (currentEndpoint.endpointUrl.match(/^(https?:\/\/)?((([a-zA-F\d]([a-zA-F\d-]*[a-zA-F\d])*)\.)+[a-zA-F]{2,}|(((\d{1,3}\.){3}\d{1,3})|(localhost))(\:\d+)?)(\/[-a-zA-F\d%_.~+]*)*(\?[;&a-zA-F\d%_.~+=-]*)?(\#[-a-zA-F\d_]*)?$/g) === null) {
        logger.error(`The 'endpointUrl' property contains some characters not allowed at the selected endpoint. If you didn't select any endpoint, the first one is taken by default.`);
    }

    if (!skillManifest.dispatchModels || skillManifest.dispatchModels === undefined) {
        logger.error(`Missing property 'dispatchModels' of the manifest`);
    }
    if (!skillManifest.activities ||  Object.keys(skillManifest.activities).length === 0) {
        logger.error(`Missing property 'activities' of the manifest`);
    }
    
}
