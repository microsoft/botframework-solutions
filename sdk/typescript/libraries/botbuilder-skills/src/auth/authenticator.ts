/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { WebRequest, WebResponse } from 'botbuilder';
import { ClaimsIdentity } from 'botframework-connector';
import { IAuthenticationProvider } from './authenticationProvider';
import { AuthHelpers } from './authHelpers';
import { IWhitelistAuthenticationProvider } from './whitelistAuthenticationProvider';

export interface IAuthenticator {
    authenticate(webRequest: WebRequest, webResponse: WebResponse): Promise<ClaimsIdentity>;
}

export class Authenticator implements IAuthenticator {

    private readonly authenticationProvider: IAuthenticationProvider;
    private readonly whiteListAuthenticationProvider: IWhitelistAuthenticationProvider;

    public constructor (
        authenticationProvider: IAuthenticationProvider,
        whitelistAuthenticationProvider: IWhitelistAuthenticationProvider
    ) {
        if (authenticationProvider === undefined) { throw new Error ('autheticationProvider is undefined'); }
        if (whitelistAuthenticationProvider === undefined) { throw new Error ('whitelistAuthenticationProvider is undefined'); }
        this.authenticationProvider = authenticationProvider;
        this.whiteListAuthenticationProvider = whitelistAuthenticationProvider;
    }

    public async authenticate(httpRequest: WebRequest, httpResponse: WebResponse): Promise<ClaimsIdentity> {
        if (httpRequest === undefined) { throw new Error('webRequest is undefined'); }
        if (httpResponse === undefined) { throw new Error('webResponse is undefined'); }

        const authorizationHeader: string = httpRequest.headers('Authorization');
        if (authorizationHeader === undefined || authorizationHeader.trim().length === 0) {
            httpResponse.status(401);
            Promise.reject();
        }

        const claimsIdentity: ClaimsIdentity = this.authenticationProvider.authenticate(authorizationHeader);
        if (claimsIdentity === undefined) {
            httpResponse.status(401);
            Promise.reject();
        }

        const appIdClaimName: string = AuthHelpers.getAppIdClaimName(claimsIdentity);
        const appId: string | null = claimsIdentity.getClaimValue(appIdClaimName);
        if (appId !== null && this.whiteListAuthenticationProvider.appsWhitelist !== undefined &&
        this.whiteListAuthenticationProvider.appsWhitelist.size > 0 &&
        !this.whiteListAuthenticationProvider.appsWhitelist.has(appId)) {
            httpResponse.status(401);
            await httpResponse.send('Skill could not allow access from calling bot.');
        }

        return claimsIdentity;
    }
}
