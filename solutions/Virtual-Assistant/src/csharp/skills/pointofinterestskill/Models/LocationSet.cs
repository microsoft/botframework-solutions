// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    /// <summary>
    /// Represents a set of locations returned by the <see cref="IGeoSpatialService"/>.
    /// </summary>
    public class LocationSet
    {
        /// <summary>
        /// Gets or sets the total estimated results.
        /// </summary>
        /// <value>
        /// The total estimated results.
        /// </value>
        [JsonProperty(PropertyName = "estimatedTotal")]
        public int EstimatedTotal { get; set; }

        /// <summary>
        /// Gets or sets the location list.
        /// </summary>
        /// <value>
        /// The location list.
        /// </value>
        [JsonProperty(PropertyName = "resources")]
        public List<Location> Locations { get; set; }
    }
}