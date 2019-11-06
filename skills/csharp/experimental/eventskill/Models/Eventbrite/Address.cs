// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace EventSkill.Models.Eventbrite
{
    public class Address
    {
        [JsonProperty("address_1")]
        public string Address1 { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("postal_code")]
        public string PostalCode { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("localized_address_display")]
        public string LocalizedAddressDisplay { get; set; }

        [JsonProperty("localized_area_display")]
        public string LocalizedAreaDisplay { get; set; }
    }
}
