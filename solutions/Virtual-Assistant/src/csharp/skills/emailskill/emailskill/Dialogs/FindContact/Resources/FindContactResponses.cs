// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace EmailSkill.Dialogs.FindContact.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class FindContactResponses
    {
        private static readonly ResponseManager _responseManager;

        static FindContactResponses()
        {
            var dir = Path.GetDirectoryName(typeof(FindContactResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\FindContact\Resources");
            _responseManager = new ResponseManager(resDir, "FindContactResponses");
        }

        // Generated accessors
        public static BotResponse PromptOneNameOneAddress => GetBotResponse();

        public static BotResponse ConfirmMultipleContactNameSinglePage => GetBotResponse();

        public static BotResponse ConfirmMultipleContactNameMultiPage => GetBotResponse();

        public static BotResponse ConfirmMultiplContactEmailSinglePage => GetBotResponse();

        public static BotResponse ConfirmMultiplContactEmailMultiPage => GetBotResponse();

        public static BotResponse UserNotFound => GetBotResponse();

        public static BotResponse UserNotFoundAgain => GetBotResponse();

        public static BotResponse EmailWelcomeMessage => GetBotResponse();

        public static BotResponse CalendarWelcomeMessage => GetBotResponse();

        public static BotResponse BeforeSendingMessage => GetBotResponse();

        public static BotResponse AlreadyFirstPage => GetBotResponse();

        public static BotResponse AlreadyLastPage => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}