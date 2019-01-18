// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace CalendarSkill.Dialogs.TimeRemain.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class TimeRemainResponses
    {
        private static readonly ResponseManager _responseManager;

        static TimeRemainResponses()
        {
            var dir = Path.GetDirectoryName(typeof(TimeRemainResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\TimeRemain\Resources");
            _responseManager = new ResponseManager(resDir, "TimeRemainResponses");
        }

        // Generated accessors
        public static BotResponse ShowNextMeetingTimeRemainingMessage => GetBotResponse();

        public static BotResponse ShowTimeRemainingMessage => GetBotResponse();

        public static BotResponse ShowNoMeetingMessage => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}