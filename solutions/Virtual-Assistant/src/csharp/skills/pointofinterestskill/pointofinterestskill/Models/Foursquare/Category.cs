// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Category
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "pluralName")]
        public string PluralName { get; set; }

        [JsonProperty(PropertyName = "shortName")]
        public string ShortName { get; set; }

        [JsonProperty(PropertyName = "primary")]
        public bool Primary { get; set; }
    }
}
