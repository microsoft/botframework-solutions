using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models.ServiceNow
{
    public class ServiceNowNotification
    {
        [JsonProperty]
        public string Id { get; set; }

        [JsonProperty]
        public string Title { get; set; }

        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty]
        public string Category { get; set; }

        [JsonProperty]
        public string Impact { get; set; }

        [JsonProperty]
        public string Urgency { get; set; }
    }
}
