// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Authentication
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class ProviderTokenResponse
    {
        public OAuthProvider AuthenticationProvider { get; set; }

        public TokenResponse TokenResponse { get; set; }
    }
}