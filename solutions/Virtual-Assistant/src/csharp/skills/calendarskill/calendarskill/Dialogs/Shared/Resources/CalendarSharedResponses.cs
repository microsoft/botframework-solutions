// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class CalendarSharedResponses : IResponseIdCollection
    {
		public const string DidntUnderstandMessage = "DidntUnderstandMessage";
		public const string ActionEnded = "ActionEnded";
		public const string CalendarErrorMessage = "CalendarErrorMessage";
		public const string CalendarErrorMessageBotProblem = "CalendarErrorMessageBotProblem";    }
}