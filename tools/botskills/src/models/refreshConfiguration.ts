/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ILogger } from '../logger';

export interface IRefreshConfiguration {
    dispatchFolder: string;
    lgLanguage: string;
    outFolder: string;
    lgOutFolder: string;
    cognitiveModelsFile: string;
    logger?: ILogger;
}
