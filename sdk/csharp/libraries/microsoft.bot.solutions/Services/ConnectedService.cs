// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Solutions.Services
{
    /// <summary>
    /// Base configuration properties for a connected service.
    /// </summary>
    public class ConnectedService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectedService"/> class.
        /// </summary>
        /// <param name="type">The connected service type.</param>
        public ConnectedService(string type)
        {
            this.Type = type;
        }

        /// <summary>
        /// Gets or sets type of the service.
        /// </summary>
        /// <value>The type of service.</value>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets user friendly name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets unique id for the service.
        /// </summary>
        /// <value>The Id of the service.</value>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets properties that are not otherwise defined.
        /// </summary>
        /// <value>The extended properties for the object.</value>
        /// <remarks>With this, properties not represented in the defined type are not dropped when
        /// the JSON object is deserialized, but are instead stored in this property. Such properties
        /// will be written to a JSON object when the instance is serialized.</remarks>
        [JsonExtensionData(ReadData = true, WriteData = true)]
#pragma warning disable CA2227 // Collection properties should be read only (this class is obsolete, we won't fix it)
        public JObject Properties { get; set; } = new JObject();
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
