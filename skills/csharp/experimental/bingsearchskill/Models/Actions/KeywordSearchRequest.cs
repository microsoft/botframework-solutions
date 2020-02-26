using Newtonsoft.Json;

namespace BingSearchSkill.Models.Actions
{
    public class KeywordSearchRequest
    {
        [JsonProperty("keyword")]
        public string Keyword { get; set; }
    }
}
