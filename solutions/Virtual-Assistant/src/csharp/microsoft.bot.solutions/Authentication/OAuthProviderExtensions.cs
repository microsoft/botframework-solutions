using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Solutions.Authentication
{
    public static class OAuthProviderExtensions
    {
        public static OAuthProvider GetAuthenticationProvider(this string providerString)
        {
            switch (providerString)
            {
                case "Azure Active Directory v2":
                    return OAuthProvider.AzureAD;
                case "Google":
                    return OAuthProvider.Google;
                case "Todoist":
                    return OAuthProvider.Todoist;
                default:
                    throw new Exception($"The given provider {providerString} could not be parsed.");
            }
        }
    }
}
