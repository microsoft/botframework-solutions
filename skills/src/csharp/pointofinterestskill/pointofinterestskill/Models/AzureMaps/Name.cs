// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    public class Name
    {
        [JsonProperty(PropertyName = "nameLocale")]
        public string NameLocale { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string NameProperty { get; set; }
    }
}