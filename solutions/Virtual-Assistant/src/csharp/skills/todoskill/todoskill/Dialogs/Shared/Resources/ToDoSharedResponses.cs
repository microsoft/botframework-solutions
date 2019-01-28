// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Resources;

namespace ToDoSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class ToDoSharedResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string DidntUnderstandMessage = "DidntUnderstandMessage";
		public const string CancellingMessage = "CancellingMessage";
		public const string NoAuth = "NoAuth";
		public const string AuthFailed = "AuthFailed";
		public const string ActionEnded = "ActionEnded";
		public const string ToDoErrorMessage = "ToDoErrorMessage";
		public const string ToDoErrorMessage_BotProblem = "ToDoErrorMessage_BotProblem";
		public const string SettingUpOneNoteMessage = "SettingUpOneNoteMessage";
		public const string AfterOneNoteSetupMessage = "AfterOneNoteSetupMessage";
		public const string SettingUpOutlookMessage = "SettingUpOutlookMessage";
		public const string AfterOutlookSetupMessage = "AfterOutlookSetupMessage";
		public const string ShowToDoTasks = "ShowToDoTasks";
		public const string AskToDoTaskIndex = "AskToDoTaskIndex";
		public const string AskToDoContentText = "AskToDoContentText";
		public const string AfterToDoTaskAdded = "AfterToDoTaskAdded";
		public const string NoTasksInList = "NoTasksInList";
		public const string SwitchListType = "SwitchListType";

    }
}