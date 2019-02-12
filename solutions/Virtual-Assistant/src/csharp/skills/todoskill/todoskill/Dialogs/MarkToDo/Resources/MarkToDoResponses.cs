// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace ToDoSkill.Dialogs.MarkToDo.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class MarkToDoResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string AfterTaskCompleted = "AfterTaskCompleted";
		public const string AfterAllTasksCompleted = "AfterAllTasksCompleted";
		public const string ListTypePrompt = "ListTypePrompt";
		public const string AskTaskIndex = "AskTaskIndex";
		public const string AskTaskIndexRetry = "AskTaskIndexRetry";
		public const string CompleteAnotherTaskPrompt = "CompleteAnotherTaskPrompt";
		public const string CompleteAnotherTaskConfirmFailed = "CompleteAnotherTaskConfirmFailed";

    }
}