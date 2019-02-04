// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class LabelLatLng
    {
        [JsonProperty(PropertyName = "label")]
        public string Label { get; set; }

        [JsonProperty(PropertyName = "lat")]
        public float Lat { get; set; }

        [JsonProperty(PropertyName = "lng")]
        public float Lng { get; set; }
    }
}
