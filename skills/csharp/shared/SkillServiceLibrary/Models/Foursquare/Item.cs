// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.Foursquare
{
    public class Item
    {
        [JsonProperty(PropertyName = "venue")]
        public Venue Venue { get; set; }
    }
}
