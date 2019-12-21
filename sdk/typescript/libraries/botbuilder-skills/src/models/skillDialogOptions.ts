/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ISkillManifest } from './manifest';

export interface ISkillDialogOptions {
    skillManifest?: ISkillManifest;
    parameters: Map<string, Object>;
}
