// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    public class Viewport
    {
        [JsonProperty(PropertyName = "topLeftPoint")]
        public LatLng TopLeftPoint { get; set; }

        [JsonProperty(PropertyName = "btmRightPoint")]
        public LatLng BtmRightPoint { get; set; }
    }
}