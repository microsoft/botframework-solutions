/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TokenResponse } from 'botframework-schema';
import { OAuthProvider } from './oAuthProvider';

export interface IProviderTokenResponse {
    authenticationProvider: OAuthProvider;
    tokenResponse: TokenResponse;
}

export function isProviderTokenResponse(value?: Object): boolean {
    return value !== undefined && (value as IProviderTokenResponse).authenticationProvider !== undefined;
}
