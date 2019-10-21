using Newtonsoft.Json;

namespace EventSkill.Models
{
    public class AugmentedLocation
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }
    }
}
