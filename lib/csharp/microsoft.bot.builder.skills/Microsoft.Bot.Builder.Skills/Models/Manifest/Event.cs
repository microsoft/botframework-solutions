using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{
    public class Event
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
