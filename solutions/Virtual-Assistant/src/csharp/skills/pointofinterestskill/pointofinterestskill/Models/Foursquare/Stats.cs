using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Stats
    {
        [JsonProperty(PropertyName = "tipCount")]
        public int TipCount { get; set; }

        [JsonProperty(PropertyName = "usersCount")]
        public int UsersCount { get; set; }

        [JsonProperty(PropertyName = "checkinsCount")]
        public int CheckinsCount { get; set; }
    }
}
