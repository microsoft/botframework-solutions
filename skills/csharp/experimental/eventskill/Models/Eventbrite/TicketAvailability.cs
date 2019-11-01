// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class TicketAvailability
    {
        [JsonProperty("has_available_tickets")]
        public bool HasAvailableTickets { get; set; }

        [JsonProperty("minimum_ticket_price")]
        public TicketPrice MinTicketPrice { get; set; }

        [JsonProperty("maximum_ticket_price")]
        public TicketPrice MaxTicketPrice { get; set; }

        [JsonProperty("is_sold_out")]
        public bool IsSoldOut { get; set; }

        [JsonProperty("start_sales_date")]
        public DateTimeTZ StartSalesDate { get; set; }

        [JsonProperty("waitlist_available")]
        public bool WaitlistAvailable { get; set; }
    }
}
