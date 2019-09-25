// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace PhoneSkill.Responses.Shared
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class PhoneSharedResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string DidntUnderstandMessage = "DidntUnderstandMessage";
        public const string CancellingMessage = "CancellingMessage";
        public const string NoAuth = "NoAuth";
        public const string AuthFailed = "AuthFailed";
        public const string ActionEnded = "ActionEnded";
        public const string ErrorMessage = "ErrorMessage";
    }
}