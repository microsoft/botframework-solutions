/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { Claim, JwtTokenValidation, SkillValidation } from 'botframework-connector';

/**
 * Sample claims validator that loads an allowed list from configuration
 * and checks that requests are coming from allowed parent bots.
 */
export class AllowedCallersClaimsValidator {
    private readonly configKey: string = 'allowedCallers';
    private readonly allowedCallers: string[];

    public constructor(allowedCallers: string[]) {
        // AllowedCallers is the setting in the appsettings.json file
        // that consists of the list of parent bot IDs that are allowed to access the skill.
        // To add a new parent bot, simply edit the AllowedCallers and add
        // the parent bot's Microsoft app ID to the list.
        // In this sample, we allow all callers if AllowedCallers contains an "*".
        if (allowedCallers === undefined) {
            throw new Error('allowedCallers parameter is undefined.');
        }

        this.allowedCallers = allowedCallers;
    }

    public async validateClaims(claims: Claim[]): Promise<void> {
        // If _allowedCallers contains an "*", we allow all callers.
        if (SkillValidation.isSkillClaim(claims) && !this.allowedCallers.includes('*')) {
            // Check that the appId claim in the skill request is in the list of callers configured for this bot.
            const appId: string = JwtTokenValidation.getAppIdFromClaims(claims);
            if (!this.allowedCallers.includes(appId)) {
                throw new Error(`Received a request from a bot with an app ID of ${ appId }. To enable requests from this caller, add the app ID to your configuration file.`);
            }
        }

        return Promise.resolve();
    }
}