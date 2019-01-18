using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Response
    {
        [JsonProperty(PropertyName = "venues")]
        public Venue[] Venues { get; set; }
    }
}
