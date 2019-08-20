using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class Logo
    {
        [JsonProperty("original")]
        public OriginalLogo Original { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
