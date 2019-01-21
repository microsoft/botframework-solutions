// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace EmailSkill.Dialogs.ConfirmRecipient.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class ConfirmRecipientResponses
    {
        private static readonly ResponseManager _responseManager;

        static ConfirmRecipientResponses()
        {
            var dir = Path.GetDirectoryName(typeof(ConfirmRecipientResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\ConfirmRecipient\Resources");
            _responseManager = new ResponseManager(resDir, "ConfirmRecipientResponses");
        }

        // Generated accessors
        public static BotResponse PromptTooManyPeople => GetBotResponse();

        public static BotResponse PromptPersonNotFound => GetBotResponse();

        public static BotResponse ConfirmRecipient => GetBotResponse();

        public static BotResponse ConfirmRecipientNotFirstPage => GetBotResponse();

        public static BotResponse ConfirmRecipientLastPage => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}