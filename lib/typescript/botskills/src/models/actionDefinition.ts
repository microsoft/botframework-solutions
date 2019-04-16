/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ISlot } from './slot';
import { ITriggers } from './triggers';

export interface IActionDefinition {
    description: string;
    slots: ISlot[];
    triggers: ITriggers;
}
