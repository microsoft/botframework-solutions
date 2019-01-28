// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace ToDoSkill.Dialogs.ShowToDo.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class ShowToDoResponses
    {
        private static readonly ResponseManager _responseManager;

        static ShowToDoResponses()
        {
            var dir = Path.GetDirectoryName(typeof(ShowToDoResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\ShowToDo\Resources");
            _responseManager = new ResponseManager(resDir, "ShowToDoResponses");
        }

        // Generated accessors
        public static BotResponse LatestOneTask => GetBotResponse();

        public static BotResponse LatestTwoTasks => GetBotResponse();

        public static BotResponse LatestThreeOrMoreTasks => GetBotResponse();

        public static BotResponse AskAddOrCompleteTaskMessage => GetBotResponse();

        public static BotResponse ReadMoreTasksPrompt => GetBotResponse();

        public static BotResponse NextOneTask => GetBotResponse();

        public static BotResponse NextTwoTasks => GetBotResponse();

        public static BotResponse NextThreeOrMoreTask => GetBotResponse();

        public static BotResponse ShowPreviousTasks => GetBotResponse();

        public static BotResponse NoTasksMessage => GetBotResponse();

        public static BotResponse InstructionMessage => GetBotResponse();

        public static BotResponse TaskSummaryMessage => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}