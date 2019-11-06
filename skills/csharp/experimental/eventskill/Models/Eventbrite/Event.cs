// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("is_free")]
        public bool IsFree { get; set; }

        [JsonProperty("venue")]
        public Venue Venue { get; set; }

        [JsonProperty("ticket_availability")]
        public TicketAvailability TicketAvailability { get; set; }

        [JsonProperty("logo")]
        public Logo Logo { get; set; }
    }
}
