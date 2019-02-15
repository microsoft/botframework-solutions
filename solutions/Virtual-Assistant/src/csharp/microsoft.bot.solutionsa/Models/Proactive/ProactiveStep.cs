// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Configuration;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Model.Proactive
{
    public class ProactiveStep : ConnectedService
    {
        public ProactiveStep()
            : base("proactiveStep")
        {
        }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("skillId")]
        public string SkillId { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, string> Parameters { get; set; }
    }
}