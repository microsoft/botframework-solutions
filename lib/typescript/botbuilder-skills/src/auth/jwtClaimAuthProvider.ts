/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ClaimsIdentity, ICredentialProvider, JwtTokenValidation } from 'botframework-connector';
import { Activity } from 'botframework-schema';
import { ISkillAuthProvider } from './skillAuthProvider';
import { ISkillWhitelist } from './skillWhitelist';

export class JwtClaimAuthProvider implements ISkillAuthProvider {
    private readonly skillWhitelist: ISkillWhitelist;
    private readonly credentialProvider: ICredentialProvider;

    constructor(skillWhitelist: ISkillWhitelist, credentialProvider: ICredentialProvider) {
        this.skillWhitelist = skillWhitelist;
        this.credentialProvider = credentialProvider;
    }

    public async authenticate(authHeader: string, activity: Activity, channelService?: string): Promise<boolean> {
        // if not provided a list, ignore this authentication operation
        if (this.skillWhitelist === undefined
            || this.skillWhitelist.skillWhiteList === undefined
            || this.skillWhitelist.skillWhiteList.length === 0) {
            return true;
        }

        try {
            const claims: ClaimsIdentity = await JwtTokenValidation.authenticateRequest(
                activity,
                authHeader,
                this.credentialProvider,
                channelService || '');
            const version: string = claims.getClaimValue('ver') || '';
            let appId: string = '';
            let appIdClaimType: string = '';

            if (version === '1.0') {
                appIdClaimType = 'appid';
            } else if (version === '2.0') {
                appIdClaimType = 'azip';
            }

            const appIdClaim: string = claims.getClaimValue(appIdClaimType) || '';
            if (appIdClaim) {
                appId = appIdClaim;
            }

            return this.skillWhitelist.skillWhiteList.includes(appId);
        } catch {
            return false;
        }
    }
}
