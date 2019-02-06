// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace AutomotiveSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class AutomotiveSkillSharedResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string DidntUnderstandMessage = "DidntUnderstandMessage";
		public const string DidntUnderstandMessageIgnoringInput = "DidntUnderstandMessageIgnoringInput";
		public const string CancellingMessage = "CancellingMessage";
		public const string ActionEnded = "ActionEnded";
		public const string ErrorMessage = "ErrorMessage";
		public const string NoAuth = "NoAuth";

    }
}