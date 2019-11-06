// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class EventSearchResult
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }

        [JsonProperty("events")]
        public List<Event> Events { get; set; }

        [JsonProperty("location")]
        public Location Location { get; set; }
    }
}
