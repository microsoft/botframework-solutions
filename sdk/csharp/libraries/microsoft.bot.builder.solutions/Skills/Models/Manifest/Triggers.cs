// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest
{
    /// <summary>
    /// Definition of the triggers for a given action within a Skill.
    /// </summary>
    public class Triggers
    {
        [JsonProperty(PropertyName = "utterances")]
        public List<Utterance> Utterances { get; set; }

        [JsonProperty(PropertyName = "utteranceSources")]
        public List<UtteranceSource> UtteranceSources { get; set; }

        [JsonProperty(PropertyName = "events")]
        public List<Event> Events { get; set; }
    }
}
