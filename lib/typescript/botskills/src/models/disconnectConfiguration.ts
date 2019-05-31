/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ILogger } from '../logger';

export interface IDisconnectConfiguration {
    skillId: string;
    skillsFile: string;
    outFolder: string;
    noTrain: boolean;
    cognitiveModelsFile: string;
    language: string;
    luisFolder: string;
    dispatchFolder: string;
    lgOutFolder: string;
    dispatchName: string;
    lgLanguage: string;
    logger?: ILogger;
}
