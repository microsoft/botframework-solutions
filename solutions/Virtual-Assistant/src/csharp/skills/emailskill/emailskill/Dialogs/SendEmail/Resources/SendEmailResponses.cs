// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace EmailSkill.Dialogs.SendEmail.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class SendEmailResponses
    {
        private static readonly ResponseManager _responseManager;

        static SendEmailResponses()
        {
            var dir = Path.GetDirectoryName(typeof(SendEmailResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\SendEmail\Resources");
            _responseManager = new ResponseManager(resDir, "SendEmailResponses");
        }

        // Generated accessors
        public static BotResponse RecipientConfirmed => GetBotResponse();

        public static BotResponse NoSubject => GetBotResponse();

        public static BotResponse NoMessageBody => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}