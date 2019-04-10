/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { SkillDefinition } from './skillDefinition';

export interface ISkillDialogOptions {

    skillDefinition: SkillDefinition | undefined;

    parameters: Map<string, Object>;
}
