/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { PromptOptions } from 'botbuilder-dialogs';
import { Activity } from 'botbuilder';
import { ISkillManifest } from '../models/manifest/skillManifest';

export class SwitchSkillDialogOptions implements PromptOptions {
    public skill?: ISkillManifest; 
    public prompt?: string | Partial<Activity>;
    
    /**
     * Initializes a new instance of the SwitchSkillDialogOptions class
     * @param prompt The Activity to display when prompting to switch skills.
     * @param manifest The SkillManifest for the new skill.
     */
    public constructor(prompt: Activity, manifest: ISkillManifest) {
        this.prompt = prompt;
        this.skill = manifest;
    }
}
