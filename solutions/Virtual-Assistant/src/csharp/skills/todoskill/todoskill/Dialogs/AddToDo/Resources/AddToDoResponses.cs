// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace ToDoSkill.Dialogs.AddToDo.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class AddToDoResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string AskTaskContentText = "AskTaskContentText";
        public const string AfterTaskAdded = "AfterTaskAdded";
        public const string SwitchListType = "SwitchListType";
        public const string SwitchListTypeConfirmFailed = "SwitchListTypeConfirmFailed";
        public const string AddMoreTask = "AddMoreTask";
        public const string AddMoreTaskConfirmFailed = "AddMoreTaskConfirmFailed";
        public const string AskAddDupTaskPrompt = "AskAddDupTaskPrompt";
        public const string AskAddDupTaskConfirmFailed = "AskAddDupTaskConfirmFailed";
    }
}