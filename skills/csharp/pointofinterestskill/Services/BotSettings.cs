// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions;

namespace PointOfInterestSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string AzureMapsKey { get; set; }

        public string FoursquareClientId { get; set; }

        public string FoursquareClientSecret { get; set; }

        public string Radius { get; set; }

        public string LimitSize { get; set; }

        public string RouteLimit { get; set; }
    }
}
