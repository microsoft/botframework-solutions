// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest
{
    /// <summary>
    /// The SkillManifest class models the Skill Manifest which is used to express the capabilities
    /// of a skill and used to drive Skill configuration and orchestration.
    /// </summary>
    public class SkillManifest : SkillOptions
    {
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "iconUrl")]
        public Uri IconUrl { get; set; }

        [JsonProperty(PropertyName = "authenticationConnections")]
        public List<AuthenticationConnection> AuthenticationConnections { get; } = new List<AuthenticationConnection>();

        [JsonProperty(PropertyName = "actions")]
        public List<Action> Actions { get; } = new List<Action>();
    }
}
