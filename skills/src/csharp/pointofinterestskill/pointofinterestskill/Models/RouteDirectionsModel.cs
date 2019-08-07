// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PointOfInterestSkill.Models
{
    public class RouteDirectionsModel : PointOfInterestModel
    {
        public string TravelTime { get; set; } = string.Empty;

        public string ETA { get; set; } = string.Empty;

        public string DelayStatus { get; set; } = string.Empty;

        public string TravelTimeSpeak { get; set; } = string.Empty;

        public string TravelDelaySpeak { get; set; } = string.Empty;

        public new string AvailableDetails { get; set; } = string.Empty;
    }
}