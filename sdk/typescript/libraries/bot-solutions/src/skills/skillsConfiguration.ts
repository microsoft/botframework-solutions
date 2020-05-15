/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IEnhancedBotFrameworkSkill } from './models/enhancedBotFrameworkSkill';

/**
 * A helper class that loads Skills information from configuration.
 */
export class SkillsConfiguration {
    public readonly skillHostEndpoint: string;
    public skills: Map<string, IEnhancedBotFrameworkSkill> = new Map<string, IEnhancedBotFrameworkSkill>();

    public constructor(skills: IEnhancedBotFrameworkSkill[], skillHostEndpoint: string) {
        
        if (skills !== undefined)
        {
            skills.forEach((skill: IEnhancedBotFrameworkSkill): void => {

                this.skills.set(skill.id, skill);
            });
        }
        
        this.skillHostEndpoint = skillHostEndpoint;
    }
}
