// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
