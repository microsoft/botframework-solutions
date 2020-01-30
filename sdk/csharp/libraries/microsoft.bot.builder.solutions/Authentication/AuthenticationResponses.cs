// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder.Solutions.Responses;

namespace Microsoft.Bot.Builder.Solutions.Authentication
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    [Obsolete("This type is being deprecated. It's moved to the assembly Microsoft.Bot.Solutions. Please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class AuthenticationResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string SkillAuthenticationTitle = "SkillAuthenticationTitle";
        public const string SkillAuthenticationPrompt = "SkillAuthenticationPrompt";
        public const string AuthProvidersPrompt = "AuthProvidersPrompt";
        public const string ConfiguredAuthProvidersPrompt = "ConfiguredAuthProvidersPrompt";
        public const string ErrorMessageAuthFailure = "ErrorMessageAuthFailure";
        public const string NoLinkedAccount = "NoLinkedAccount";
    }
}