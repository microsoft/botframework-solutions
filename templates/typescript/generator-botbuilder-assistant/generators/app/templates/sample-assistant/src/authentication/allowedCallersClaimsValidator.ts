/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Claim, JwtTokenValidation, SkillValidation } from 'botframework-connector';
import { SkillsConfiguration } from 'botbuilder-solutions';

/**
 * Sample claims validator that loads an allowed list from configuration if present and checks that responses are coming from configured skills.
 */
export class AllowedCallersClaimsValidator {
    private readonly allowedSkills: string[];

    public constructor(skillsConfig: SkillsConfiguration) {
        if (skillsConfig === undefined) {
            throw new Error ('The value of skillsConfig is undefined');
        }

        // Load the appIds for the configured skills (we will only allow responses from skills we have configured).
        this.allowedSkills = [...skillsConfig.skills.values()].map(skill => skill.appId);
    }

    public async validateClaims(claims: Claim[]): Promise<void> {
        if (SkillValidation.isSkillClaim(claims)) {
            // Check that the appId claim in the skill request is in the list of skills configured for this bot.
            const appId = JwtTokenValidation.getAppIdFromClaims(claims);
            if (!this.allowedSkills.includes(appId)) {
                throw new Error(`Received a request from a bot with an app ID of "${ appId }". To enable requests from this caller, add the app ID to your configuration file.`);
            }
        }

        return Promise.resolve();
    }
}
