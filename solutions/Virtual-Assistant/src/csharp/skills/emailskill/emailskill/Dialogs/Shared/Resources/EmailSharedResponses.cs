// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace EmailSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class EmailSharedResponses
    {
        private static readonly ResponseManager _responseManager;

        static EmailSharedResponses()
        {
            var dir = Path.GetDirectoryName(typeof(EmailSharedResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\Shared\Resources");
            _responseManager = new ResponseManager(resDir, "EmailSharedResponses");
        }

        // Generated accessors
        public static BotResponse DidntUnderstandMessage => GetBotResponse();

        public static BotResponse DidntUnderstandMessageIgnoringInput => GetBotResponse();

        public static BotResponse CancellingMessage => GetBotResponse();

        public static BotResponse NoAuth => GetBotResponse();

        public static BotResponse AuthFailed => GetBotResponse();

        public static BotResponse ActionEnded => GetBotResponse();

        public static BotResponse EmailErrorMessage => GetBotResponse();

        public static BotResponse EmailErrorMessage_BotProblem => GetBotResponse();

        public static BotResponse SentSuccessfully => GetBotResponse();

        public static BotResponse NoRecipients => GetBotResponse();

        public static BotResponse NoEmailContent => GetBotResponse();

        public static BotResponse RecipientConfirmed => GetBotResponse();

        public static BotResponse ConfirmSend => GetBotResponse();

        public static BotResponse ConfirmSendFailed => GetBotResponse();

        public static BotResponse EmailNotFound => GetBotResponse();

        public static BotResponse NoFocusMessage => GetBotResponse();

        public static BotResponse ShowEmailPrompt => GetBotResponse();

        public static BotResponse ShowOneEmailPrompt => GetBotResponse();

        public static BotResponse NoChoiceOptions_Retry => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}