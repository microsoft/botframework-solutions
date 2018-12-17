namespace RestaurantBooking.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class Meeting
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("date")]
        public DateTime? Date { get; set; }

        [JsonProperty("attendees")]
        public List<string> Attendees { get; set; }

        [JsonProperty("time")]
        public DateTime? Time { get; set; }
    }
}
