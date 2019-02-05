// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;


namespace PointOfInterestSkill.Models.Foursquare
{
    public class Item
    {
        [JsonProperty(PropertyName = "venue")]
        public Venue Venue { get; set; }
    }
}
