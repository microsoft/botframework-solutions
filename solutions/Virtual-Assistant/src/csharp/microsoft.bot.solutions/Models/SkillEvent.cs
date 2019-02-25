// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Configuration;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Model
{
    public class SkillEvent : ConnectedService
    {
        public SkillEvent()
            : base("skillEvent")
        {
        }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("skillIds")]
        public string[] SkillIds { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, string> Parameters { get; set; }
    }
}