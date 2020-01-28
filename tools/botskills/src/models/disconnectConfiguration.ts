/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ILogger } from '../logger';

export interface IDisconnectConfiguration {
    skillId: string;
    outFolder: string;
    noRefresh: boolean;
    cognitiveModelsFile: string;
    languages: string[];
    dispatchFolder: string;
    lgOutFolder: string;
    lgLanguage: string;
    logger?: ILogger;
    appSettingsFile: string;
}
