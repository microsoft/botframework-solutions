// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Connector.Authentication;

namespace Microsoft.Bot.Builder.Solutions.Skills.Auth
{
    public class MicrosoftGovernmentAppCredentialsEx : MicrosoftGovernmentAppCredentials, IServiceClientCredentials
    {
        public MicrosoftGovernmentAppCredentialsEx(string appId, string password)
            : base(appId, password)
        {
        }
    }
}