// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class Viewport
    {
        [JsonProperty(PropertyName = "topLeftPoint")]
        public LatLng TopLeftPoint { get; set; }

        [JsonProperty(PropertyName = "btmRightPoint")]
        public LatLng BtmRightPoint { get; set; }
    }
}