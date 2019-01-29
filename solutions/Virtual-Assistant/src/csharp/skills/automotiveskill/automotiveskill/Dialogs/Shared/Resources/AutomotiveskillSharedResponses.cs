// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace AutomotiveSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class AutomotiveSkillSharedResponses
    {
        private static readonly ResponseManager _responseManager;

        static AutomotiveSkillSharedResponses()
        {
            var dir = Path.GetDirectoryName(typeof(AutomotiveSkillSharedResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\Shared\Resources");
            _responseManager = new ResponseManager(resDir, "AutomotiveSkillSharedResponses");
        }

        // Generated accessors
        public static BotResponse DidntUnderstandMessage => GetBotResponse();

        public static BotResponse DidntUnderstandMessageIgnoringInput => GetBotResponse();

        public static BotResponse CancellingMessage => GetBotResponse();

        public static BotResponse ActionEnded => GetBotResponse();

        public static BotResponse ErrorMessage => GetBotResponse();

        public static BotResponse NoAuth => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}