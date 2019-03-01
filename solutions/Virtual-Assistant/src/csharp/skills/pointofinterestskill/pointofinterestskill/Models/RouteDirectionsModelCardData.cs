// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Solutions.Responses;

namespace PointOfInterestSkill.Models
{
    public class RouteDirectionsModelCardData : ICardData
    {
        public string Name { get; set; }

        public string Hours { get; set; }

        public string Street { get; set; }

        public string City { get; set; }

        public string AvailableDetails { get; set; }

        public string ImageUrl { get; set; }

        public string TravelTime { get; set; }

        public string ETA { get; set; }

        public string Distance { get; set; }

        public string DelayStatus { get; set; }

        public string TravelTimeSpeak { get; set; }

        public string TravelDelaySpeak { get; set; }
    }
}