using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class Venue
    {
        [JsonProperty("address")]
        public Address Address { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
