/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { EnhancedBotFrameworkSkill } from './enhancedBotFrameworkSkill';

/**
 * A helper class that loads Skills information from configuration.
 */
export class SkillsConfiguration {
    public readonly skillHostEndpoint: string;
    public skills: Map<string, EnhancedBotFrameworkSkill> = new Map<string, EnhancedBotFrameworkSkill>();

    public constructor(skills: EnhancedBotFrameworkSkill[], skillHostEndpoint: string) {
        
        if (skills !== undefined)
        {
            skills.forEach((skill: EnhancedBotFrameworkSkill): void => {

                this.skills.set(skill.id, skill);
            });
        }
        
        this.skillHostEndpoint = skillHostEndpoint;
    }
}
