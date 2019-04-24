/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ILogger } from '../logger';

export interface IDisconnectConfiguration {
    skillName: string;
    skillsFile: string;
    logger?: ILogger;
}
