// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Icon
    {
        [JsonProperty(PropertyName = "prefix")]
        public string Prefix { get; set; }

        [JsonProperty(PropertyName = "summary")]
        public int[] Sizes { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
