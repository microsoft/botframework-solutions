// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models.Foursquare
{
    public class Hours
    {
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "richStatus")]
        public Richstatus RichStatus { get; set; }

        [JsonProperty(PropertyName = "isOpen")]
        public bool IsOpen { get; set; }

        [JsonProperty(PropertyName = "isLocalHoliday")]
        public bool IsLocalHoliday { get; set; }

        [JsonProperty(PropertyName = "dayData")]
        public object[] DayData { get; set; }
    }
}
