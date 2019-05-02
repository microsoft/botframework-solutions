/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { MicrosoftAppCredentials } from 'botframework-connector';

export class MicrosoftAppCredentialsEx extends MicrosoftAppCredentials {
    constructor(appId: string, password: string, oauthScope?: string) {
        super(appId, password);
        if (oauthScope) {
            this.oAuthScope = oauthScope;
        }
        
        this.oAuthEndpoint = 'https://login.microsoftonline.com/microsoft.com';
    }
}
