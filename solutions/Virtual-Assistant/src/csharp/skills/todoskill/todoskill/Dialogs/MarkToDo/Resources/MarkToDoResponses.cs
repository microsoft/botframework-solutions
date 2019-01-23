// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace ToDoSkill.Dialogs.MarkToDo.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class MarkToDoResponses
    {
        private static readonly ResponseManager _responseManager;

        static MarkToDoResponses()
        {
            var dir = Path.GetDirectoryName(typeof(MarkToDoResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\MarkToDo\Resources");
            _responseManager = new ResponseManager(resDir, "MarkToDoResponses");
        }

        // Generated accessors
        public static BotResponse AfterTaskCompleted => GetBotResponse();

        public static BotResponse AfterAllTasksCompleted => GetBotResponse();

        public static BotResponse ListTypePrompt => GetBotResponse();

        public static BotResponse AskTaskIndex => GetBotResponse();

        public static BotResponse CompleteAnotherTaskPrompt => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}