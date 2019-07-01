using Newtonsoft.Json;

namespace NewsSkill.Models
{
    public class SearchAddress
    {
        /// <summary>
        /// Gets or sets a string specifying the country of an address.
        /// </summary>
        /// <value>
        /// A string specifying the country of an address.
        /// </value>
        [JsonProperty(PropertyName = "countryCode")]
        public string CountryCode { get; set; }
    }
}
