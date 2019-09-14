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
        public List<Utterance> Utterances { get; } = new List<Utterance>();

        [JsonProperty(PropertyName = "utteranceSources")]
        public List<UtteranceSource> UtteranceSources { get; } = new List<UtteranceSource>();

        [JsonProperty(PropertyName = "events")]
        public List<Event> Events { get; } = new List<Event>();
    }
}
