// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace PointOfInterestSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class POISharedResponses
    {
        private static readonly ResponseManager _responseManager;

        static POISharedResponses()
        {
            var dir = Path.GetDirectoryName(typeof(POISharedResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\Shared\Resources");
            _responseManager = new ResponseManager(resDir, "POISharedResponses");
        }

        // Generated accessors
        public static BotResponse DidntUnderstandMessage => GetBotResponse();

        public static BotResponse CancellingMessage => GetBotResponse();

        public static BotResponse NoAuth => GetBotResponse();

        public static BotResponse AuthFailed => GetBotResponse();

        public static BotResponse ActionEnded => GetBotResponse();

        public static BotResponse PointOfInterestErrorMessage => GetBotResponse();

        public static BotResponse PromptToGetRoute => GetBotResponse();

        public static BotResponse GetRouteToActiveLocationLater => GetBotResponse();

        public static BotResponse MultipleLocationsFound => GetBotResponse();

        public static BotResponse SingleLocationFound => GetBotResponse();

        public static BotResponse MultipleLocationsFoundAlongActiveRoute => GetBotResponse();

        public static BotResponse SingleLocationFoundAlongActiveRoute => GetBotResponse();

        public static BotResponse NoLocationsFound => GetBotResponse();

        public static BotResponse MultipleRoutesFound => GetBotResponse();

        public static BotResponse SingleRouteFound => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}