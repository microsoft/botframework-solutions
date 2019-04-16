/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IActionDefinition } from './actionDefinition';

export interface IAction {
    id: string;
    definition: IActionDefinition;
}
