// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Solutions.Responses;

namespace PointOfInterestSkill.Models
{
    public class LocationCardModelData : ICardData
    {
        public string ImageUrl { get; set; }

        public string LocationName { get; set; }

        public string Address { get; set; }

        public string SpeakAddress { get; set; }

        public string ActionText { get; set; }

        public int OptionNumber { get; set; }
    }
}