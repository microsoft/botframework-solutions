// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Response
    {
        [JsonProperty(PropertyName = "venues")]
        public Venue[] Venues { get; set; }

        [JsonProperty(PropertyName = "venue")]
        public Venue Venue { get; set; }

        [JsonProperty(PropertyName = "groups")]
        public Group[] Groups { get; set; }
    }
}
