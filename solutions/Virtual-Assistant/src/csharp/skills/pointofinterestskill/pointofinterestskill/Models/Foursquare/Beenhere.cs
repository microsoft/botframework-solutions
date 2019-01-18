using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Beenhere
    {
        [JsonProperty(PropertyName = "lastCheckinExpiredAt")]
        public int LastCheckinExpiredAt { get; set; }
    }

}
