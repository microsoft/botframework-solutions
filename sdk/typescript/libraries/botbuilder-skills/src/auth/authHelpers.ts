/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { ClaimsIdentity } from 'botframework-connector';

export namespace AuthHelpers {

    export function getAppIdClaimName (claimsIdentity: ClaimsIdentity): string {

        if (claimsIdentity === undefined) { throw new Error('ClaimsIdentity is undefined'); }
        // version "1.0" tokens include the "appid" claim and version "2.0" tokens include the "azp" claim
        let appIdClaimName: string = 'appid';
        const tokenVersion: string | null = claimsIdentity.getClaimValue('ver');
        if (tokenVersion === '2.0') {
            appIdClaimName = 'azp';
        }

        return appIdClaimName;
    }
}
