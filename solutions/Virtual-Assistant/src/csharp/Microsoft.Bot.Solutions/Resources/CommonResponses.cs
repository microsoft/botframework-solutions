// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace Microsoft.Bot.Solutions.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class CommonResponses
    {
        private static readonly ResponseManager _responseManager;

        static CommonResponses()
        {
            var dir = Path.GetDirectoryName(typeof(CommonResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Resources");
            _responseManager = new ResponseManager(resDir, "CommonResponses");
        }

        // Generated accessors
        public static BotResponse ConfirmUserInfo => GetBotResponse();

        public static BotResponse ConfirmSaveInfoFailed => GetBotResponse();

        public static BotResponse ErrorMessage => GetBotResponse();

        public static BotResponse ErrorMessage_AuthFailure => GetBotResponse();

        public static BotResponse ErrorMessage_SkillError => GetBotResponse();

        public static BotResponse SkillAuthenticationTitle => GetBotResponse();

        public static BotResponse SkillAuthenticationPrompt => GetBotResponse();

        public static BotResponse AuthProvidersPrompt => GetBotResponse();

        public static BotResponse ConfiguredAuthProvidersPrompt => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}