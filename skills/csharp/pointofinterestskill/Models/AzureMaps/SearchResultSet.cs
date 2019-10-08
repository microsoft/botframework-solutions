// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
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