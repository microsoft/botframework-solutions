// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Authentication
{
    public enum OAuthProvider
    {
        /// <summary>
        /// Azure Activity Directory authentication provider.
        /// </summary>
        AzureAD,

        /// <summary>
        /// Google authentication provider.
        /// </summary>
        Google,

        /// <summary>
        /// Todoist authentication provider.
        /// </summary>
        Todoist,

        /// <summary>
        /// Generic Oauth 2 provider.
        /// </summary>
        GenericOauth2,
    }
}