// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace ToDoSkill.Dialogs.ShowToDo.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class ShowToDoResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string LatestTask = "LatestTask";
		public const string LatestTasks = "LatestTasks";
		public const string MostRecentTasks = "MostRecentTasks";
		public const string AskAddOrCompleteTaskMessage = "AskAddOrCompleteTaskMessage";
		public const string ReadMoreTasksPrompt = "ReadMoreTasksPrompt";
		public const string ReadMoreTasksConfirmFailed = "ReadMoreTasksConfirmFailed";
		public const string ReadMoreTasksPrompt2 = "ReadMoreTasksPrompt2";
		public const string RetryReadMoreTasksPrompt2 = "RetryReadMoreTasksPrompt2";
		public const string NextTask = "NextTask";
		public const string NextTasks = "NextTasks";
		public const string PreviousTasks = "PreviousTasks";
		public const string PreviousFirstTasks = "PreviousFirstTasks";
		public const string PreviousFirstSingleTask = "PreviousFirstSingleTask";
		public const string NoTasksMessage = "NoTasksMessage";
		public const string TaskSummaryMessage = "TaskSummaryMessage";
		public const string RepeatFirstPagePrompt = "RepeatFirstPagePrompt";
		public const string RepeatFirstPageConfirmFailed = "RepeatFirstPageConfirmFailed";
		public const string GoBackToStartPromptForSingleTask = "GoBackToStartPromptForSingleTask";
		public const string GoBackToStartForSingleTaskConfirmFailed = "GoBackToStartForSingleTaskConfirmFailed";
		public const string GoBackToStartPromptForTasks = "GoBackToStartPromptForTasks";
		public const string GoBackToStartForTasksConfirmFailed = "GoBackToStartForTasksConfirmFailed";

    }
}