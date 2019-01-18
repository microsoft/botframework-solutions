// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    /// <summary>
    /// Represents the location returned from the Bing Geo Spatial API:
    /// https://msdn.microsoft.com/en-us/library/ff701725.aspx.
    /// </summary>
    public class Location
    {
        ///// <summary>
        ///// Gets or sets the location type.
        ///// </summary>
        [JsonProperty(PropertyName = "__type")]
        public string LocationType { get; set; }

        /// <summary>
        /// Gets or sets a geographic area that contains the location. A bounding box contains SouthLatitude,
        /// WestLongitude, NorthLatitude, and EastLongitude values in units of degrees.
        /// </summary>
        /// <value>
        /// A geographic area that contains the location. A bounding box contains SouthLatitude,
        /// WestLongitude, NorthLatitude, and EastLongitude values in units of degrees.
        /// </value>
        [JsonProperty(PropertyName = "bbox")]
        public List<double> BoundaryBox { get; set; }

        /// <summary>
        /// Gets or sets the name of the resource.
        /// </summary>
        /// <value>
        /// The name of the resource.
        /// </value>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the latitude and longitude coordinates of the location.
        /// </summary>
        /// <value>
        /// The latitude and longitude coordinates of the location.
        /// </value>
        [JsonProperty(PropertyName = "point")]
        public GeocodePoint Point { get; set; }

        /// <summary>
        /// Gets or sets the postal address for the location. An address can contain AddressLine, Neighborhood,
        /// Locality, AdminDistrict, AdminDistrict2, CountryRegion, CountryRegionIso2, PostalCode,
        /// FormattedAddress, and Landmark fields.
        /// </summary>
        /// <value>
        /// The postal address for the location. An address can contain AddressLine, Neighborhood,
        /// Locality, AdminDistrict, AdminDistrict2, CountryRegion, CountryRegionIso2, PostalCode,
        /// FormattedAddress, and Landmark fields.
        /// </value>
        [JsonProperty(PropertyName = "address")]
        public Address Address { get; set; }

        ///// <summary>
        ///// Gets or sets the level of confidence that the geocoded location result is a match.
        ///// Use this value with the match code to determine for more complete information about the match.
        ///// </summary>
        [JsonProperty(PropertyName = "confidence")]
        public string Confidence { get; set; }

        /// <summary>
        /// Gets or sets the classification of the geographic entity returned, such as Address.
        /// For a list of entity types, see Location and Area Types.
        /// </summary>
        /// <value>
        /// The classification of the geographic entity returned, such as Address.
        /// For a list of entity types, see Location and Area Types.
        /// </value>
        [JsonProperty(PropertyName = "entityType")]
        public string EntityType { get; set; }

        /// <summary>
        /// Gets or sets a collection of geocoded points that differ in how they were calculated and their suggested use.
        /// For a description of the points in this collection, see the Geocode Point Fields section below.
        /// </summary>
        /// <value>
        /// A collection of geocoded points that differ in how they were calculated and their suggested use.
        /// For a description of the points in this collection, see the Geocode Point Fields section below.
        /// </value>
        [JsonProperty(PropertyName = "geocodePoints")]
        public List<GeocodePoint> GeocodePoints { get; set; }

        ///// <summary>
        ///// One or more match code values that represent the geocoding level for each location in the response.
        ///// </summary>
        [JsonProperty(PropertyName = "matchCodes")]
        public List<string> MatchCodes { get; set; }
    }
}