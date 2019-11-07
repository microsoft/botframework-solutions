﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Connector.Authentication;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    public class MicrosoftAppCredentialsEx : MicrosoftAppCredentials, IServiceClientCredentials
    {
        private readonly string _oauthScope;

        public MicrosoftAppCredentialsEx(string appId, string password, string oauthScope)
            : base(appId, password)
        {
            _oauthScope = oauthScope;
        }

        public override string OAuthScope => _oauthScope;

        public override string OAuthEndpoint => "https://login.microsoftonline.com/microsoft.com";
    }
}