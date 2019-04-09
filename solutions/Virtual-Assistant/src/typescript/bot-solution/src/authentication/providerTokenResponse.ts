/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TokenResponse } from 'botframework-schema';

export interface IProviderTokenResponse {
    authenticationProvider: OAuthProvider;
    tokenResponse: TokenResponse;
}

export function isProviderTokenResponse(value: Object): boolean {
    return (<IProviderTokenResponse>value).authenticationProvider !== undefined;
}

export function getAuthenticationProvider(provider: string): OAuthProvider {
    switch (provider) {
        case OAuthProvider.AzureAD.toString():
            return OAuthProvider.AzureAD;
        case OAuthProvider.Google.toString():
            return OAuthProvider.Google;
        case OAuthProvider.Todoist.toString():
            return OAuthProvider.Todoist;
        default:
            throw new Error(`The given provider '${provider}' could not be parsed.`);
    }
}

export enum OAuthProvider {
    AzureAD = 'Azure Active Directory v2',
    Google = 'Google',
    Todoist = 'Todoist'
}
