// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace Microsoft.Bot.Solutions.Authentication
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class AuthenticationResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string SkillAuthenticationTitle = "SkillAuthenticationTitle";
        public const string SkillAuthenticationPrompt = "SkillAuthenticationPrompt";
        public const string AuthProvidersPrompt = "AuthProvidersPrompt";
        public const string ConfiguredAuthProvidersPrompt = "ConfiguredAuthProvidersPrompt";
        public const string ErrorMessageAuthFailure = "ErrorMessageAuthFailure";
        public const string NoLinkedAccount = "NoLinkedAccount";
        public const string LoginButton = "LoginButton";
        public const string LoginPrompt = "LoginPrompt";
    }
}