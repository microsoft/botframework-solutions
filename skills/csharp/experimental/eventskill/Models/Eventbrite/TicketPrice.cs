// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class TicketPrice
    {
        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("major_value")]
        public string MajorValue { get; set; }

        [JsonProperty("display")]
        public string Display { get; set; }
    }
}
