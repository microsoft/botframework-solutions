// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace CalendarSkill.Dialogs.ChangeEventStatus.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class ChangeEventStatusResponses
    {
        private static readonly ResponseManager _responseManager;

        static ChangeEventStatusResponses()
        {
            var dir = Path.GetDirectoryName(typeof(ChangeEventStatusResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\ChangeEventStatus\Resources");
            _responseManager = new ResponseManager(resDir, "ChangeEventStatusResponses");
        }

        // Generated accessors
        public static BotResponse ConfirmDelete => GetBotResponse();

        public static BotResponse ConfirmDeleteFailed => GetBotResponse();

        public static BotResponse ConfirmAccept => GetBotResponse();

        public static BotResponse ConfirmAcceptFailed => GetBotResponse();

        public static BotResponse EventDeleted => GetBotResponse();

        public static BotResponse EventAccepted => GetBotResponse();

        public static BotResponse EventWithStartTimeNotFound => GetBotResponse();

        public static BotResponse NoDeleteStartTime => GetBotResponse();

        public static BotResponse NoAcceptStartTime => GetBotResponse();

        public static BotResponse MultipleEventsStartAtSameTime => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}