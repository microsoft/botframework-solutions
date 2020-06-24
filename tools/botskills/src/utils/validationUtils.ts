/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ISkillManifestV1 } from '../models/manifestV1/skillManifestV1';
import { ISkillManifestV2 } from '../models/manifestV2/skillManifestV2';
import { ILogger } from '../logger';
import { ChildProcessUtils } from '../utils';

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
    if (skillManifest.name === undefined || skillManifest.name === '') {
        logger.error(`Missing property 'name' of the manifest`);
    }

    if (skillManifest.id === undefined || skillManifest.id === '') {
        logger.error(`Missing property 'id' of the manifest`);
    } else if (skillManifest.id.match(/^\d|[^\w]/g) !== null) {
        logger.error(`The 'id' of the manifest contains some characters not allowed. Make sure the 'id' contains only letters, numbers and underscores, but doesn't start with number.`);
    }

    if (skillManifest.endpoint === undefined || skillManifest.endpoint === '') {
        logger.error(`Missing property 'endpoint' of the manifest`);
    } else if (skillManifest.endpoint.match(/^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?$/g) === null) {
        logger.error(`The 'endpoint' property contains some characters not allowed.`);
    }

    if (skillManifest.authenticationConnections === undefined) {
        logger.error(`Missing property 'authenticationConnections' of the manifest`);
    }

    if (skillManifest.actions === undefined || skillManifest.actions[0] === undefined) {
        logger.error(`Missing property 'actions' of the manifest`);
    }

}

export function manifestV2Validation(skillManifest: ISkillManifestV2, logger: ILogger, endpointName?: string): void {
    if (skillManifest.$schema === undefined || skillManifest.$schema === '') {
        logger.error(`Missing property '$schema' of the manifest`);
    }

    if (skillManifest.$id === undefined || skillManifest.$id === '') {
        logger.error(`Missing property '$id' of the manifest`);
    } else if (skillManifest.$id.match(/^\d|[^\w]/g) !== null) {
        logger.error(`The '$id' of the manifest contains some characters not allowed. Make sure the '$id' contains only letters, numbers and underscores, but doesn't start with number.`);
    }

    if (skillManifest.endpoints === undefined || skillManifest.endpoints.length === 0) {
        logger.error(`Missing property 'endpoints' of the manifest`);
    } else {
        let currentEndpoint = skillManifest.endpoints.find((endpoint): boolean =>  endpoint.name == endpointName) || skillManifest.endpoints[0];
        if (currentEndpoint.name === undefined || currentEndpoint.name === ''){
            logger.error(`Missing property 'name' at the selected endpoint. If you didn't select any endpoint, the first one is taken by default`);
        }

        if (currentEndpoint.msAppId === undefined || currentEndpoint.msAppId === ''){
            logger.error(`Missing property 'msAppId' at the selected endpoint. If you didn't select any endpoint, the first one is taken by default`);
        } else if (currentEndpoint.msAppId.match(/^[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}$/g) === null) {
            logger.error(`The 'msAppId' property of the selected endpoint contains invalid characters or does not comply with the GUID format. If you didn't select any endpoint, the first one is taken by default.`);
        }

        if (currentEndpoint.endpointUrl === undefined || currentEndpoint.endpointUrl === ''){
            logger.error(`Missing property 'endpointUrl' at the selected endpoint. If you didn't select any endpoint, the first one is taken by default`);
        } else if (currentEndpoint.endpointUrl.match(/^(ht|f)tp(s?)\:\/\/[0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*(:(0-9)*)*(\/?)([a-zA-Z0-9\-\.\?\,\'\/\\\+&amp;%\$#_]*)?$/g) === null) {
            logger.error(`The 'endpointUrl' property of the selected endpoint contains invalid characters or does not comply with the URL format. If you didn't select any endpoint, the first one is taken by default.`);
        }
    }

    if (skillManifest.dispatchModels === undefined || Object.keys(skillManifest.dispatchModels).length === 0) {
        logger.error(`Missing property 'dispatchModels' of the manifest`);
    }

    if (!skillManifest.activities || Object.keys(skillManifest.activities).length === 0) {
        logger.error(`Missing property 'activities' of the manifest`);
    }
}

export async function validateLibrary(libs: libraries[], logger: ILogger): Promise<void> {
    await Promise.all(libs.map(async (library: libraries) => {
        const lib: libraryCommand = commands[library];
        await new ChildProcessUtils().execute(lib.cmd, lib.args).catch( err => {
            logger.error(`You are missing the library ${ libraries[library] }. Please visit ${ lib.package }.`);
        });
    }));
}

export enum libraries {
    BotFrameworkCLI
}

interface libraryCommand {
    cmd: string;
    args: string[];
    package: string;
}

const commands: { [key: number]: libraryCommand } = {
    0: { cmd: 'bf', args: ['-v'], package: 'https://www.npmjs.com/package/@microsoft/botframework-cli' }
};
