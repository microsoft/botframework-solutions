using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class MultipartText
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("html")]
        public string Html { get; set; }
    }
}
