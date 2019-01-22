// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public class Richstatus
    {
        [JsonProperty(PropertyName = "entities")]
        public object[] Entities { get; set; }

        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }
}
