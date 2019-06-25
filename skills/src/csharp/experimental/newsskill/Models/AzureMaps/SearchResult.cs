using Newtonsoft.Json;

namespace NewsSkill.Models
{
    public class SearchResult
    {
        /// <summary>
        /// Gets or sets EntityType string address.
        /// </summary>
        /// <value>
        /// The result type.
        /// </value>
        [JsonProperty(PropertyName = "address")]
        public SearchAddress Address { get; set; }
    }
}
