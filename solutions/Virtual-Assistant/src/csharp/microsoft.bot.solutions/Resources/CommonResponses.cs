// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace Microsoft.Bot.Solutions.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class CommonResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string ConfirmUserInfo = "ConfirmUserInfo";
		public const string ConfirmSaveInfoFailed = "ConfirmSaveInfoFailed";
		public const string ErrorMessage = "ErrorMessage";
		public const string ErrorMessage_AuthFailure = "ErrorMessage_AuthFailure";
		public const string ErrorMessage_SkillError = "ErrorMessage_SkillError";
		public const string SkillAuthenticationTitle = "SkillAuthenticationTitle";
		public const string SkillAuthenticationPrompt = "SkillAuthenticationPrompt";
		public const string AuthProvidersPrompt = "AuthProvidersPrompt";
		public const string ConfiguredAuthProvidersPrompt = "ConfiguredAuthProvidersPrompt";

    }
}