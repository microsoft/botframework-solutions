// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    public class PoiInfo
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}