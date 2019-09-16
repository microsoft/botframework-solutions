using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VirtualAssistantSample.Models
{
    public class SummaryResultModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("items")]
        public List<Item> Items { get; set; }

        public class Item
        {
            [JsonProperty("title")]
            public string Title { get; set; }
        }
    }
}
