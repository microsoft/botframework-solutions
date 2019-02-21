// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    public class SearchResult
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string ResultType { get; set; }

        [JsonProperty(PropertyName = "address")]
        public SearchAddress Address { get; set; }

        [JsonProperty(PropertyName = "position")]
        public LatLng Position { get; set; }

        [JsonProperty(PropertyName = "poi")]
        public PoiInfo Poi { get; set; }

        [JsonProperty(PropertyName = "viewport")]
        public Viewport Viewport { get; set; }
    }
}