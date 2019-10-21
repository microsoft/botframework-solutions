// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    /// <summary>
    /// Represents a geo point.
    /// </summary>
    public class GeocodePoint
    {
        /// <summary>
        /// Gets or sets list of the coordinates.
        /// </summary>
        /// <value>
        /// List of the coordinates.
        /// </value>
        [JsonProperty(PropertyName = "coordinates")]
        public List<double> Coordinates { get; set; }

        /// <summary>
        /// Gets or sets  the method that was used to compute the geocode point.
        /// </summary>
        /// <value>
        /// the method that was used to compute the geocode point.
        /// </value>
        [JsonProperty(PropertyName = "calculationMethod")]
        public string CalculationMethod { get; set; }

        /// <summary>
        /// Gets or sets the best use for the geocode point.
        /// </summary>
        /// <value>
        /// The best use for the geocode point.
        /// </value>
        [JsonProperty(PropertyName = "usageTypes")]
        public List<string> UsageTypes { get; set; }

        /// <summary>
        /// Gets a value indicating whether point has geo coordinates or not.
        /// </summary>
        /// <value>
        /// Returns whether point has geo coordinates or not.
        /// </value>
        [JsonIgnore]
        public bool HasCoordinates => Coordinates != null && Coordinates.Count == 2;
    }
}