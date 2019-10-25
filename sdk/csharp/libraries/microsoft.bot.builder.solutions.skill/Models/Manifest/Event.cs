using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Solutions.Skill.Models.Manifest
{
    public class Event
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
