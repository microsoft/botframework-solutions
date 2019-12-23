/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IActionDefinition } from './';

export interface IAction {
    id: string;
    definition: IActionDefinition;
}
