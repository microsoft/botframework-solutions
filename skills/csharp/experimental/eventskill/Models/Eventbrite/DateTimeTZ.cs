// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class DateTimeTZ
    {
        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        [JsonProperty("local")]
        public DateTime Local { get; set; }

        [JsonProperty("utc")]
        public DateTime Utc { get; set; }
    }
}
