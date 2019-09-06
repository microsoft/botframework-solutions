// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{
#pragma warning disable CA1716 // Identifiers should not match keywords (disable, this class will be removed)
    public class Event
#pragma warning restore CA1716 // Identifiers should not match keywords
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
