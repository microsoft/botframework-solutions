// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace PointOfInterestSkill.Dialogs.CancelRoute.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class CancelRouteResponses
    {
        private static readonly ResponseManager _responseManager;

        static CancelRouteResponses()
        {
            var dir = Path.GetDirectoryName(typeof(CancelRouteResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\CancelRoute\Resources");
            _responseManager = new ResponseManager(resDir, "CancelRouteResponses");
        }

        // Generated accessors
        public static BotResponse CancelActiveRoute => GetBotResponse();

        public static BotResponse CannotCancelActiveRoute => GetBotResponse();

        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}