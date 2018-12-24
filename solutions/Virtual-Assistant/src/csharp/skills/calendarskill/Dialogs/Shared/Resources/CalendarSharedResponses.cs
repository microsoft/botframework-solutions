// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace CalendarSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class CalendarSharedResponses
    {
        private static readonly ResponseManager _responseManager;

        static CalendarSharedResponses()
        {
            var dir = Path.GetDirectoryName(typeof(CalendarSharedResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\Shared\Resources");
            _responseManager = new ResponseManager(resDir, "CalendarSharedResponses");
        }

        // Generated accessors
        public static BotResponse DidntUnderstandMessage => GetBotResponse();

        public static BotResponse ActionEnded => GetBotResponse();

        public static BotResponse CalendarErrorMessage => GetBotResponse();

        public static BotResponse CalendarErrorMessageBotProblem => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}