// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Solutions.Dialogs;

namespace PointOfInterestSkill.Dialogs.FindPointOfInterest.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public static class FindPointOfInterestResponses
    {
        private static readonly ResponseManager _responseManager;

        static FindPointOfInterestResponses()
        {
            var dir = Path.GetDirectoryName(typeof(FindPointOfInterestResponses).Assembly.Location);
            var resDir = Path.Combine(dir, @"Dialogs\FindPointOfInterest\Resources");
            _responseManager = new ResponseManager(resDir, "FindPointOfInterestResponses");
        }

        // Generated accessors
        private static BotResponse GetBotResponse([CallerMemberName] string propertyName = null)
        {
            return _responseManager.GetBotResponse(propertyName);
        }
    }
}