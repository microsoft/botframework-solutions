using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{
    /// <summary>
    /// Slot definition for a given Action within a Skill manifest.
    /// </summary>
    public class Slot
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "types")]
        public string[] Types { get; set; }
    }
}
