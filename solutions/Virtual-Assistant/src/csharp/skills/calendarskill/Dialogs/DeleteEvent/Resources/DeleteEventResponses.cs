// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace CalendarSkill.Dialogs.DeleteEvent.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class DeleteEventResponses
    {
        private static readonly ResponseManager _responseManager;

		static DeleteEventResponses()
		{
			var dir = Path.GetDirectoryName(typeof(DeleteEventResponses).Assembly.Location);
			var resDir = Path.Combine(dir, "Dialogs", "DeleteEvent", "Resources");
			_responseManager = new ResponseManager(resDir, "DeleteEventResponses");
		}

        // Generated accessors
        public static BotResponse ConfirmDelete => GetBotResponse();

        public static BotResponse ConfirmDeleteFailed => GetBotResponse();

        public static BotResponse EventDeleted => GetBotResponse();

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