using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models
{
    // Temporary class to represent settings for skill
    // Should be part of Manifest
    public class SkillSettings
    {
        [JsonProperty("Skill")]
        public SkillConfiguration SkillConfig { get; set; }

        public class SkillConfiguration
        {
            public bool IsMsJWTAuthenticationEnabled { get; set; }
        }
    }
}