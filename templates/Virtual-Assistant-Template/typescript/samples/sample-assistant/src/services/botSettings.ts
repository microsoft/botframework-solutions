/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */
import { ISkillManifest } from 'botbuilder-skills';
import { IBotSettingsBase } from 'botbuilder-solutions';

export interface IBotSettings extends IBotSettingsBase {
    skills: ISkillManifest[];
}
