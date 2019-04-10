using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills.Models.Manifest
{
    /// <summary>
    /// Describes an Authentication connection that a Skill requires for operation.
    /// </summary>
    public class AuthenticationConnection
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "serviceProviderId")]
        public string ServiceProviderId { get; set; }

        [JsonProperty(PropertyName = "scopes")]
        public string Scopes { get; set; }
    }
}
