// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace EmailSkill.Dialogs.ForwardEmail.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class ForwardEmailResponses
    {
        private static readonly ResponseManager _responseManager;

        static ForwardEmailResponses()
        {
            var dir = Path.GetDirectoryName(typeof(ForwardEmailResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\ForwardEmail\Resources");
            _responseManager = new ResponseManager(resDir, "ForwardEmailResponses");
        }

        // Generated accessors
        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}