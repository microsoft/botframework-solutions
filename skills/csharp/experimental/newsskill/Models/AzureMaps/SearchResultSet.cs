using System.Collections.Generic;
using Newtonsoft.Json;

namespace NewsSkill.Models
{
    public class SearchResultSet
    {
        /// <summary>
        /// Gets or sets the location list.
        /// </summary>
        /// <value>
        /// The location list.
        /// </value>
        [JsonProperty(PropertyName = "results")]
        public List<SearchResult> Results { get; set; }
    }
}
