// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Solutions.Authentication
{
    public static class OAuthProviderExtensions
    {
        public static OAuthProvider GetAuthenticationProvider(this string providerString)
        {
            switch (providerString)
			{
				case "Azure Active Directory":
				case "Azure Active Directory v2":
                    return OAuthProvider.AzureAD;
                case "Google":
                    return OAuthProvider.Google;
                case "Todoist":
                    return OAuthProvider.Todoist;
                case "Generic Oauth 2":
                case "Oauth 2 Generic Provider":
                    return OAuthProvider.GenericOauth2;
                default:
                    throw new Exception($"The given provider {providerString} could not be parsed.");
            }
        }
    }
}