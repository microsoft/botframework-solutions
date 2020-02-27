/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { PromptOptions } from "botbuilder-dialogs";
import { Activity } from "botbuilder";
import { ISkillManifest } from '../models/manifest/skillManifest';
import { EnhancedBotFrameworkSkill } from "../enhancedBotFrameworkSkill";

export class SwitchSkillDialogOptions implements PromptOptions {
    public skill?: EnhancedBotFrameworkSkill; 
    public prompt?: string | Partial<Activity>;
    
    /**
     * Initializes a new instance of the SwitchSkillDialogOptions class
     * @param prompt The Activity to display when prompting to switch skills.
     * @param skill The EnhancedBotFrameworkSkill for the new skill.
     */
    public constructor(prompt: Activity, skill: EnhancedBotFrameworkSkill) {
        this.prompt = prompt;
        this.skill = skill;
    }
}
