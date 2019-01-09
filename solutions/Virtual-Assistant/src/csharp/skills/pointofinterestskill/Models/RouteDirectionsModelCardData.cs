// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Solutions.Cards;

namespace PointOfInterestSkill.Models
{
    public class RouteDirectionsModelCardData : CardDataBase
    {
        public string Location { get; set; }

        public string TravelTime { get; set; }

        public string TrafficDelay { get; set; }

        public int RouteId { get; set; }
    }
}