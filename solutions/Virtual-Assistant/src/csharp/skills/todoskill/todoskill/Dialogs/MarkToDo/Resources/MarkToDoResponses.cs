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
		public const string AfterToDoTaskCompleted = "AfterToDoTaskCompleted";
		public const string AfterAllToDoTasksCompleted = "AfterAllToDoTasksCompleted";    }
}