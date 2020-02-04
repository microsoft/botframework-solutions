/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { MicrosoftAppCredentials } from 'botframework-connector';
import { IServiceClientCredentials } from './serviceClientCredentials';

export class MicrosoftAppCredentialsEx extends MicrosoftAppCredentials implements IServiceClientCredentials {

    // PENDING: In C# this property is not added in this class although it's in IServiceClientCredentials.
    // We should take a look if we should remove it or not.
    // https://github.com/microsoft/botframework-solutions/blob/1e21a1dce5220cc35de258dc84b1df5c4eeb2f5e/lib/csharp/microsoft.bot.builder.skills/Microsoft.Bot.Builder.Skills/Auth/IServiceClientCredentials.cs#L9
    public microsoftAppId: string = '';

    public constructor(appId: string, password: string, oauthScope?: string) {
        super(appId, password);
        if (oauthScope) {
            this.oAuthScope = oauthScope;
        }
        this.oAuthEndpoint = 'https://login.microsoftonline.com/microsoft.com';
    }
}
