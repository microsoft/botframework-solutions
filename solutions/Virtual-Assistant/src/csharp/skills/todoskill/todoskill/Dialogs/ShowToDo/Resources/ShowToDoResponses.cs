// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Resources;

namespace ToDoSkill.Dialogs.ShowToDo.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class ShowToDoResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string FirstToDoTasks = "FirstToDoTasks";
		public const string ShowNextToDoTasks = "ShowNextToDoTasks";
		public const string ShowPreviousToDoTasks = "ShowPreviousToDoTasks";
		public const string ShowingMoreTasks = "ShowingMoreTasks";
		public const string NoToDoTasksPrompt = "NoToDoTasksPrompt";

    }
}