// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace PointOfInterestSkill.Dialogs.Route.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class RouteResponses
    {
        private static readonly ResponseManager _responseManager;

        static RouteResponses()
        {
            var dir = Path.GetDirectoryName(typeof(RouteResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\Route\Resources");
            _responseManager = new ResponseManager(resDir, "RouteResponses");
        }

        // Generated accessors
        public static BotResponse MissingActiveLocationErrorMessage => GetBotResponse();

        public static BotResponse PromptToStartRoute => GetBotResponse();

        public static BotResponse SendingRouteDetails => GetBotResponse();

        public static BotResponse AskAboutRouteLater => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}