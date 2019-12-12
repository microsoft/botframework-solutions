// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SkillServiceLibrary.Models
{
    public class RouteDirectionsModel : PointOfInterestModel
    {
        public string TravelTime { get; set; }

        public string ETA { get; set; }

        public string DelayStatus { get; set; }

        public string TravelTimeSpeak { get; set; }

        public string TravelDelaySpeak { get; set; }

        public new string AvailableDetails { get; set; }
    }
}