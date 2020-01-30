// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest
{
    /// <summary>
    /// Definition of a Manifest Axtion. Describes how an actio is trigger and any slots (parameters) it accepts.
    /// </summary>
    [Obsolete("This type is being deprecated. To continue using Skill capability please refer to https://aka.ms/botframework-solutions/releases/0_8", false)]
    public class ActionDefinition
    {
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "slots")]
        public List<Slot> Slots { get; set; } = new List<Slot>();

        [JsonProperty(PropertyName = "response")]
        public dynamic Response { get; set; }

        [JsonProperty(PropertyName = "triggers")]
        public Triggers Triggers { get; set; }
    }
}