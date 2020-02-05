/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ILogger } from '../logger';

export interface IConnectConfiguration {
    botName: string;
    localManifest: string;
    remoteManifest: string;
    endpointName: string;
    noRefresh: boolean;
    languages: string[];
    luisFolder: string;
    dispatchFolder: string;
    outFolder: string;
    lgOutFolder: string;
    resourceGroup: string;
    appSettingsFile: string;
    cognitiveModelsFile: string;
    lgLanguage: string;
    logger?: ILogger;
}
