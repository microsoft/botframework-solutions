using System;
using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class Event
    {
        [JsonProperty("name")]
        public MultipartText Name { get; set; }

        [JsonProperty("description")]
        public MultipartText Description { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("start")]
        public DateTimeTZ Start { get; set; }

        [JsonProperty("end")]
        public DateTimeTZ End { get; set; }

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("changed")]
        public DateTime Changed { get; set; }

        [JsonProperty("published")]
        public DateTime Published { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("online_event")]
        public bool OnlineEvent { get; set; }

        [JsonProperty("hide_start_date")]
        public bool HideStartDate { get; set; }

        [JsonProperty("hide_end_date")]
        public bool HideEndDate { get; set; }

        [JsonProperty("logo")]
        public Logo Logo { get; set; }
    }
}
