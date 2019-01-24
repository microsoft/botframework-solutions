// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace CalendarSkill.Dialogs.CreateEvent.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class CreateEventResponses
    {
        private static readonly ResponseManager _responseManager;

        static CreateEventResponses()
        {
            var dir = Path.GetDirectoryName(typeof(CreateEventResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\CreateEvent\Resources");
            _responseManager = new ResponseManager(resDir, "CreateEventResponses");
        }

        // Generated accessors
        public static BotResponse NoTitle => GetBotResponse();

        public static BotResponse NoTitle_Short => GetBotResponse();

        public static BotResponse NoContent => GetBotResponse();

        public static BotResponse NoLocation => GetBotResponse();

        public static BotResponse ConfirmCreate => GetBotResponse();

        public static BotResponse ConfirmCreateFailed => GetBotResponse();

        public static BotResponse EventCreated => GetBotResponse();

        public static BotResponse EventCreationFailed => GetBotResponse();

        public static BotResponse NoAttendees => GetBotResponse();

        public static BotResponse PromptTooManyPeople => GetBotResponse();

        public static BotResponse PromptPersonNotFound => GetBotResponse();

        public static BotResponse NoStartDate => GetBotResponse();

        public static BotResponse NoStartDate_Retry => GetBotResponse();

        public static BotResponse NoStartTime => GetBotResponse();

        public static BotResponse NoStartTime_Retry => GetBotResponse();

        public static BotResponse NoStartTime_NoSkip => GetBotResponse();

        public static BotResponse NoDuration => GetBotResponse();

        public static BotResponse NoDuration_Retry => GetBotResponse();

        public static BotResponse GetRecreateInfo => GetBotResponse();

        public static BotResponse GetRecreateInfo_Retry => GetBotResponse();

        public static BotResponse ConfirmRecipient => GetBotResponse();

        public static BotResponse InvaildDuration => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}