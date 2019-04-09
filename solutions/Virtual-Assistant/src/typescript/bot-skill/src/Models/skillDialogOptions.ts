import { SkillDefinition } from './skillDefinition';

/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

export interface ISkillDialogOptions {

   skillDefinition: SkillDefinition | undefined;
   parameters: Map<string, Object>;
}
