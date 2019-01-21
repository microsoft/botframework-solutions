using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{

    public class Source
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
    }
}