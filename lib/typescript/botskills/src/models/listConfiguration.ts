/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ILogger } from '../logger';

export interface IListConfiguration {
    skillsFile: string;
    logger?: ILogger;
}
