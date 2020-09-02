// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Configuration.Encryption;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Services
{
    /// <summary>
    /// Configuration properties for a connected LUIS Service.
    /// </summary>
    public class LuisService : ConnectedService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuisService"/> class.
        /// </summary>
        public LuisService()
            : base(ServiceTypes.Luis)
        {
        }

        /// <summary>
        /// Gets or sets appId for the LUIS model.
        /// </summary>
        /// <value>The App Id.</value>
        [JsonProperty("appId")]
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets authoringKey for interacting with service management.
        /// </summary>
        /// <value>The Authoring Key.</value>
        [JsonProperty("authoringKey")]
        public string AuthoringKey { get; set; }

        /// <summary>
        /// Gets or sets subscriptionKey for accessing this service.
        /// </summary>
        /// <value>The Subscription Key.</value>
        [JsonProperty("subscriptionKey")]
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets version of the LUIS app.
        /// </summary>
        /// <value>The Version of the LUIS app.</value>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets region.
        /// </summary>
        /// <value>The Region.</value>
        [JsonProperty("region")]
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the URL for a custom endpoint. This should only be used when the LUIS deployed via a container.
        /// If a value is set, then the GetEndpoint() method will return the value for Custom Endpoint.
        /// </summary>
        /// <value>The Region.</value>
        [JsonProperty("customEndpoint")]
        public string CustomEndpoint { get; set; }

        /// <summary>
        /// Gets the endpoint for this LUIS service.
        /// </summary>
        /// <returns>The URL for this service.</returns>
        public string GetEndpoint()
        {
            // If a custom endpoint has been supplied, then we should return this instead of
            // generating an endpoint based on the region.
            if (!string.IsNullOrEmpty(this.CustomEndpoint))
            {
                return this.CustomEndpoint;
            }

            if (string.IsNullOrWhiteSpace(this.Region))
            {
                throw new System.NullReferenceException("LuisService.Region cannot be Null");
            }

#pragma warning disable CA1304 // Specify CultureInfo (this class is obsolete, we won't fix it)
            var region = this.Region.ToLower();
#pragma warning restore CA1304 // Specify CultureInfo

            // usgovvirginia is that actual azure region name, but the cognitive service team called their endpoint 'virginia' instead of 'usgovvirginia'
            // We handle both region names as an alias for virginia.api.cognitive.microsoft.us
            if (region == "virginia" || region == "usgovvirginia")
            {
                return $"https://virginia.api.cognitive.microsoft.us";
            }

            // if it starts with usgov or usdod then it is a .us TLD
#pragma warning disable CA1307 // Specify StringComparison (this class is obsolete, we won't fix it)
            else if (region.StartsWith("usgov") || region.StartsWith("usdod"))
#pragma warning restore CA1307 // Specify StringComparison
            {
                return $"https://{this.Region}.api.cognitive.microsoft.us";
            }

            return $"https://{this.Region}.api.cognitive.microsoft.com";
        }
    }
}
