using Newtonsoft.Json;

namespace BingSearchSkill.Models.Actions
{
    public class KeywordSearchResponse
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
