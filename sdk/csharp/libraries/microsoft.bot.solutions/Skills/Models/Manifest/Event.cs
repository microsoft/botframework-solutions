// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Skills.Models.Manifest
{
    public class Event
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
