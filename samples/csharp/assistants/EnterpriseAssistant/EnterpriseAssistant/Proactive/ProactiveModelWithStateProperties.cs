using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualAssistant.Proactive
{
    public class ProactiveModelWithStateProperties
    {
        /// <summary>
        /// Gets or sets the sanitized Id/Key used as PrimaryKey.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the un-sanitized Id/Key.
        /// </summary>
        /// <remarks>
        /// Note: There is a Typo in the property name ("ReadlId"), that can't be changed due to compatability concerns. The
        /// Json is correct due to the JsonProperty field, but the Typo needs to stay.
        /// </remarks>
        // DO NOT FIX THE TYPO BELOW (See Remarks above).
        [JsonProperty("realId")]
        public string RealId { get; internal set; }

        /// <summary>
        /// Gets or sets the persisted object.
        /// </summary>
        [JsonProperty("document")]
        public JObject Document { get; set; }

        /// <summary>
        /// Gets or sets the ETag information for handling optimistic concurrency updates.
        /// </summary>
        [JsonProperty("_etag")]
        public string ETag { get; set; }
    }
}