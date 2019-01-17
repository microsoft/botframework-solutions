// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace EmailSkill.Dialogs.DeleteEmail.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class DeleteEmailResponses
    {
        private static readonly ResponseManager _responseManager;

        static DeleteEmailResponses()
        {
            var dir = Path.GetDirectoryName(typeof(DeleteEmailResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\DeleteEmail\Resources");
            _responseManager = new ResponseManager(resDir, "DeleteEmailResponses");
        }

        // Generated accessors
        public static BotResponse DeletePrompt => GetBotResponse();

        public static BotResponse DeleteConfirm => GetBotResponse();

        public static BotResponse DeleteSuccessfully => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}