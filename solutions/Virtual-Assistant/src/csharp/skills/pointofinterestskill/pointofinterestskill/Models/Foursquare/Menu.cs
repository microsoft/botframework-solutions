using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Menu
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "label")]
        public string Label { get; set; }

        [JsonProperty(PropertyName = "anchor")]
        public string Anchor { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "mobileUrl")]
        public string MobileUrl { get; set; }

        [JsonProperty(PropertyName = "externalUrl")]
        public string ExternalUrl { get; set; }
    }
}
