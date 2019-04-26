/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ISkillManifest } from './skillManifest';

export interface ISkillDialogOptions {
   skillManifest: ISkillManifest | undefined;
   parameters: Map<string, Object>;
}
