// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace EmailSkill.Dialogs.ShowEmail.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class ShowEmailResponses
    {
        private static readonly ResponseManager _responseManager;

        static ShowEmailResponses()
        {
            var dir = Path.GetDirectoryName(typeof(ShowEmailResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\ShowEmail\Resources");
            _responseManager = new ResponseManager(resDir, "ShowEmailResponses");
        }

        // Generated accessors
        public static BotResponse ReadOutMessage => GetBotResponse();

        public static BotResponse ReadOutMorePrompt => GetBotResponse();

        public static BotResponse ReadOutOnlyOnePrompt => GetBotResponse();

        public static BotResponse ReadOutPrompt => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}