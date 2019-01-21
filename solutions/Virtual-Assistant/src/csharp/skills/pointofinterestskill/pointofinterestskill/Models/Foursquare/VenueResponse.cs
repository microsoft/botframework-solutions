using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class VenueResponse
    {
        [JsonProperty(PropertyName = "response")]
        public Response Response { get; set; }
    }
}
