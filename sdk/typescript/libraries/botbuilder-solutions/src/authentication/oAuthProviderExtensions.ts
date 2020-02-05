/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { OAuthProvider } from './oAuthProvider';

// eslint-disable-next-line @typescript-eslint/no-namespace
export namespace OAuthProviderExtensions {

    export function getAuthenticationProvider(provider: string): OAuthProvider {
        switch (provider) {
            case 'Azure Active Directory':
            case 'Azure Active Directory v2':
                return OAuthProvider.AzureAD;
            case 'Google':
                return OAuthProvider.Google;
            case 'Todoist':
                return OAuthProvider.Todoist;
            case 'Generic Oauth 2':
            case 'Oauth 2 Generic Provider':
                return OAuthProvider.GenericOauth2;
            default:
                throw new Error(`The given provider '${ provider }' could not be parsed.`);
        }
    }
}
