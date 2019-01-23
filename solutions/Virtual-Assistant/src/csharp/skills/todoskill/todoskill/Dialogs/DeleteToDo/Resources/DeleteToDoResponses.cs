// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace ToDoSkill.Dialogs.DeleteToDo.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class DeleteToDoResponses
    {
        private static readonly ResponseManager _responseManager;

        static DeleteToDoResponses()
        {
            var dir = Path.GetDirectoryName(typeof(DeleteToDoResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\DeleteToDo\Resources");
            _responseManager = new ResponseManager(resDir, "DeleteToDoResponses");
        }

        // Generated accessors
        public static BotResponse AskDeletionAllConfirmation => GetBotResponse();

        public static BotResponse AfterTaskDeleted => GetBotResponse();

        public static BotResponse AfterAllTasksDeleted => GetBotResponse();

        public static BotResponse DeletionAllConfirmationRefused => GetBotResponse();

        public static BotResponse ListTypePrompt => GetBotResponse();

        public static BotResponse AskTaskIndex => GetBotResponse();

        public static BotResponse DeleteAnotherTaskPrompt => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}