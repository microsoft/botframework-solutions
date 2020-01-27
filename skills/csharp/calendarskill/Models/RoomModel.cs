using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CalendarSkill.Models
{
    public class RoomModel
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "displayName")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "emailAddress")]
        public string EmailAddress { get; set; }

        [JsonProperty(PropertyName = "capacity")]
        public int? Capacity { get; set; }

        [JsonProperty(PropertyName = "building")]
        public string Building { get; set; }

        [JsonProperty(PropertyName = "floorNumber")]
        public int? FloorNumber { get; set; }
    }
}
