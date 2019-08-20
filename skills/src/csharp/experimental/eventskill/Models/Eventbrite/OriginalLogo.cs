using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class OriginalLogo
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }
}
