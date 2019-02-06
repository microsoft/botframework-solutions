// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace FakeSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class SharedResponses
    {
        private static readonly ResponseManager _responseManager;

        static SharedResponses()
        {
            var dir = Path.GetDirectoryName(typeof(SharedResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Skills\Fakes\FakeSkill\Dialogs\Shared\Resources");
            _responseManager = new ResponseManager(resDir, "SharedResponses");
        }

        // Generated accessors
        public static BotResponse DidntUnderstandMessage => GetBotResponse();

        public static BotResponse CancellingMessage => GetBotResponse();

        public static BotResponse NoAuth => GetBotResponse();

        public static BotResponse AuthFailed => GetBotResponse();

        public static BotResponse ActionEnded => GetBotResponse();

        public static BotResponse ErrorMessage => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}