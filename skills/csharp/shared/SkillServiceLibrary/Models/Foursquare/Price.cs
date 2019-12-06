// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.Foursquare
{
    public class Price
    {
        [JsonProperty(PropertyName = "tier")]
        public int Tier { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}
