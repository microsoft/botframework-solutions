using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Icon
    {
        [JsonProperty(PropertyName = "prefix")]
        public string Prefix { get; set; }

        [JsonProperty(PropertyName = "summary")]
        public int[] Sizes { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
