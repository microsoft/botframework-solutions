// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Price
    {
        [JsonProperty(PropertyName = "tier")]
        public int Tier { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}
