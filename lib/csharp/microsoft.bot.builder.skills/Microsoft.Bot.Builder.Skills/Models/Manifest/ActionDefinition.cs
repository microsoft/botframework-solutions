// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest
{
    /// <summary>
    /// Definition of a Manifest Action. Describes how an action is trigger and any slots (parameters) it accepts.
    /// </summary>
    public class ActionDefinition
    {
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "slots")]
        public List<Slot> Slots { get; } = new List<Slot>();

        [JsonProperty(PropertyName = "response")]
        public dynamic Response { get; set; }

        [JsonProperty(PropertyName = "triggers")]
        public Triggers Triggers { get; set; }
    }
}
