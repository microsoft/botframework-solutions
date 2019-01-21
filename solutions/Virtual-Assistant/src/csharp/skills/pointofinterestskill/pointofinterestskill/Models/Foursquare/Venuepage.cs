using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class VenuePage
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
