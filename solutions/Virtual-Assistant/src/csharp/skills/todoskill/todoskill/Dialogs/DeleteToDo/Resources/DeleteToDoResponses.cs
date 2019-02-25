// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace ToDoSkill.Dialogs.DeleteToDo.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class DeleteToDoResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string AskDeletionAllConfirmation = "AskDeletionAllConfirmation";
		public const string AskDeletionAllConfirmationFailed = "AskDeletionAllConfirmationFailed";
		public const string AfterTaskDeleted = "AfterTaskDeleted";
		public const string AfterAllTasksDeleted = "AfterAllTasksDeleted";
		public const string DeletionAllConfirmationRefused = "DeletionAllConfirmationRefused";
		public const string ListTypePromptForDelete = "ListTypePromptForDelete";
		public const string AskTaskIndexForDelete = "AskTaskIndexForDelete";
		public const string AskTaskIndexRetryForDelete = "AskTaskIndexRetryForDelete";
		public const string DeleteAnotherTaskPrompt = "DeleteAnotherTaskPrompt";
		public const string DeleteAnotherTaskConfirmFailed = "DeleteAnotherTaskConfirmFailed";

    }
}