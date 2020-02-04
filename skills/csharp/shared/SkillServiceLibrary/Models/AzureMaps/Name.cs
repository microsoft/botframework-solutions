// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class Name
    {
        [JsonProperty(PropertyName = "nameLocale")]
        public string NameLocale { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string NameProperty { get; set; }
    }
}