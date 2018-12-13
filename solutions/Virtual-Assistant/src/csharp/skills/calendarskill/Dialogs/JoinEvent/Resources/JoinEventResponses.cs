// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace CalendarSkill.Dialogs.JoinEvent.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class JoinEventResponses
    {
        private static readonly ResponseManager _responseManager;

        static JoinEventResponses()
        {
            var dir = Path.GetDirectoryName(typeof(JoinEventResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\JoinEvent\Resources");
            _responseManager = new ResponseManager(resDir, "JoinEventResponses");
        }

        // Generated accessors
        public static BotResponse NoMeetingTimeProvided => GetBotResponse();

        public static BotResponse MeetingNotFound => GetBotResponse();

        public static BotResponse NoDialInNumber => GetBotResponse();

        public static BotResponse CallingIn => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}