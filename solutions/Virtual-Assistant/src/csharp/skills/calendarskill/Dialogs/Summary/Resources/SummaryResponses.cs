// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace CalendarSkill.Dialogs.Summary.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class SummaryResponses
    {
        private static readonly ResponseManager _responseManager;

		static SummaryResponses()
		{
			var dir = Path.GetDirectoryName(typeof(SummaryResponses).Assembly.Location);
			var resDir = Path.Combine(dir, "Dialogs", "Summary", "Resources");
			_responseManager = new ResponseManager(resDir, "SummaryResponses");
		}

        // Generated accessors
        public static BotResponse CalendarNoMoreEvent => GetBotResponse();

        public static BotResponse CalendarNoPreviousEvent => GetBotResponse();

        public static BotResponse ShowNoMeetingMessage => GetBotResponse();

        public static BotResponse ShowOneMeetingSummaryMessage => GetBotResponse();

        public static BotResponse ShowMultipleMeetingSummaryMessage => GetBotResponse();

        public static BotResponse ReadOutPrompt => GetBotResponse();

        public static BotResponse ReadOutMorePrompt => GetBotResponse();

        public static BotResponse ReadOutMessage => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}