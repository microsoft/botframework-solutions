// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace CalendarSkill.Dialogs.UpdateEvent.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class UpdateEventResponses
    {
        private static readonly ResponseManager _responseManager;

        static UpdateEventResponses()
        {
            var dir = Path.GetDirectoryName(typeof(UpdateEventResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\UpdateEvent\Resources");
            _responseManager = new ResponseManager(resDir, "UpdateEventResponses");
        }

        // Generated accessors
        public static BotResponse NotEventOrganizer => GetBotResponse();

        public static BotResponse ConfirmUpdate => GetBotResponse();

        public static BotResponse ConfirmUpdateFailed => GetBotResponse();

        public static BotResponse EventUpdated => GetBotResponse();

        public static BotResponse NoNewTime => GetBotResponse();

        public static BotResponse NoNewTime_Retry => GetBotResponse();

        public static BotResponse EventWithStartTimeNotFound => GetBotResponse();

        public static BotResponse NoDeleteStartTime => GetBotResponse();

        public static BotResponse NoUpdateStartTime => GetBotResponse();

        public static BotResponse MultipleEventsStartAtSameTime => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}