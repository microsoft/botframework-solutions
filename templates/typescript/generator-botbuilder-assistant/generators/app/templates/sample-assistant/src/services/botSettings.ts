/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { IBotSettingsBase, ISkillManifest } from 'botbuilder-solutions';

export interface IBotSettings extends IBotSettingsBase {
    skills: ISkillManifest[];
}
