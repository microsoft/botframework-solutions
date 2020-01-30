// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Bot.Builder.Solutions.Authentication
{
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
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