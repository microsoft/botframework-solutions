// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace ToDoSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class ToDoSharedResponses
    {
        private static readonly ResponseManager _responseManager;

        static ToDoSharedResponses()
        {
            var dir = Path.GetDirectoryName(typeof(ToDoSharedResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\Shared\Resources");
            _responseManager = new ResponseManager(resDir, "ToDoSharedResponses");
        }

        // Generated accessors
        public static BotResponse NoAuth => GetBotResponse();

        public static BotResponse ActionEnded => GetBotResponse();

        public static BotResponse ToDoErrorMessage => GetBotResponse();

        public static BotResponse ToDoErrorMessage_BotProblem => GetBotResponse();

        public static BotResponse SettingUpOneNoteMessage => GetBotResponse();

        public static BotResponse AfterOneNoteSetupMessage => GetBotResponse();

        public static BotResponse SettingUpOutlookMessage => GetBotResponse();

        public static BotResponse AfterOutlookSetupMessage => GetBotResponse();

        public static BotResponse CardSummaryMessage => GetBotResponse();

        public static BotResponse NoTasksInList => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}