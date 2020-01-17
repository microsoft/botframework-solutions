// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Skills.Models.Manifest
{
    public class Action
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "definition")]
        public ActionDefinition Definition { get; set; }
    }
}
