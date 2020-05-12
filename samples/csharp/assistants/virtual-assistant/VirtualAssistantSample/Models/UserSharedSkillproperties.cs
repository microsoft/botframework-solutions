// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualAssistantSample.Models
{
    public class UserSharedSkillproperties
    {
        [JsonProperty("UserSharedData")]
        public SharedData[] UserSharedData { get; set; }
    }

    public partial class SharedData
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("properties")]
        public Properties Properties { get; set; }
    }

    public partial class Properties
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("permission")]
        public string Permission { get; set; }
    }
}
