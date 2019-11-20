// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace EventSkill.Models
{
    public class Location
    {
        [JsonProperty("latitude")]
        public string Latitude { get; set; }

        [JsonProperty("augmented_location")]
        public AugmentedLocation AugmentedLocation { get; set; }

        [JsonProperty("within")]
        public string Within { get; set; }

        [JsonProperty("longitude")]
        public string Longitude { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }
    }
}
