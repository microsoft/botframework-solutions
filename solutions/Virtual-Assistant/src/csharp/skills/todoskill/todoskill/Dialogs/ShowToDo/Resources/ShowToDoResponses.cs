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
		public const string LatestOneTask = "LatestOneTask";
		public const string LatestTwoTasks = "LatestTwoTasks";
		public const string LatestThreeOrMoreTasks = "LatestThreeOrMoreTasks";
		public const string AskAddOrCompleteTaskMessage = "AskAddOrCompleteTaskMessage";
		public const string ReadMoreTasksPrompt = "ReadMoreTasksPrompt";
		public const string ReadMoreTasksConfirmFailed = "ReadMoreTasksConfirmFailed";
		public const string NextOneTask = "NextOneTask";
		public const string NextTwoTasks = "NextTwoTasks";
		public const string NextThreeOrMoreTask = "NextThreeOrMoreTask";
		public const string ShowPreviousTasks = "ShowPreviousTasks";
		public const string NoTasksMessage = "NoTasksMessage";
		public const string InstructionMessage = "InstructionMessage";
		public const string TaskSummaryMessage = "TaskSummaryMessage";
		public const string RepeatFirstPagePrompt = "RepeatFirstPagePrompt";
		public const string RepeatFirstPageConfirmFailed = "RepeatFirstPageConfirmFailed";
		public const string GoBackToStartPrompt = "GoBackToStartPrompt";
		public const string GoBackToStartConfirmFailed = "GoBackToStartConfirmFailed";

    }
}