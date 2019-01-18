// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace EmailSkill.Dialogs.ReplyEmail.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class ReplyEmailResponses
    {
        private static readonly ResponseManager _responseManager;

        static ReplyEmailResponses()
        {
            var dir = Path.GetDirectoryName(typeof(ReplyEmailResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\ReplyEmail\Resources");
            _responseManager = new ResponseManager(resDir, "ReplyEmailResponses");
        }

        // Generated accessors
        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}