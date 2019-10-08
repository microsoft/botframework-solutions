using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class Logo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
