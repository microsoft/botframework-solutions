using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{
    /// <summary>
    /// The SkillManifest class models the Skill Manifest which is used to express the capabilities
    /// of a skill and used to drive Skill configuration and orchestration.
    /// </summary>
    public class SkillManifest
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "msaAppId")]
        public string MSAappId { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "endpoint")]
        public Uri Endpoint { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "iconUrl")]
        public Uri IconUrl { get; set; }

        [JsonProperty(PropertyName = "authenticationConnections")]
        public AuthenticationConnection[] AuthenticationConnections { get; set; }

        [JsonProperty(PropertyName = "actions")]
        public List<Action> Actions { get; set; } = new List<Action>();
    }
}
