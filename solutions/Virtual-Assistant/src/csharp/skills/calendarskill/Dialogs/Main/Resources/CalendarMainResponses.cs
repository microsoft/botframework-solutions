// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace CalendarSkill.Dialogs.Main.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class CalendarMainResponses
    {
        private static readonly ResponseManager _responseManager;

        static CalendarMainResponses()
        {
            var dir = Path.GetDirectoryName(typeof(CalendarMainResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\Main\Resources");
            _responseManager = new ResponseManager(resDir, "CalendarMainResponses");
        }

        // Generated accessors
        public static BotResponse CalendarWelcomeMessage => GetBotResponse();

        public static BotResponse HelpMessage => GetBotResponse();

        public static BotResponse GreetingMessage => GetBotResponse();

        public static BotResponse GoodbyeMessage => GetBotResponse();

        public static BotResponse LogOut => GetBotResponse();

        public static BotResponse FeatureNotAvailable => GetBotResponse();

        public static BotResponse CancelMessage => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}