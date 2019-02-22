// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace ToDoSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class ToDoSharedResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string NoAuth = "NoAuth";
		public const string ActionEnded = "ActionEnded";
		public const string ToDoErrorMessage = "ToDoErrorMessage";
		public const string ToDoErrorMessage_BotProblem = "ToDoErrorMessage_BotProblem";
		public const string SettingUpOneNoteMessage = "SettingUpOneNoteMessage";
		public const string AfterOneNoteSetupMessage = "AfterOneNoteSetupMessage";
		public const string SettingUpOutlookMessage = "SettingUpOutlookMessage";
		public const string AfterOutlookSetupMessage = "AfterOutlookSetupMessage";
		public const string CardSummaryMessageForMultipleTasks = "CardSummaryMessageForMultipleTasks";
		public const string CardSummaryMessageForSingleTask = "CardSummaryMessageForSingleTask";
		public const string NoTasksInList = "NoTasksInList";

    }
}