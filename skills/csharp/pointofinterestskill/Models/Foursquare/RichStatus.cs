// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Richstatus
    {
        [JsonProperty(PropertyName = "entities")]
        public object[] Entities { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }
}
