// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using PointOfInterestSkill.Models;

namespace PointOfInterestSkill.ServiceClients
{
    /// <summary>
    /// Represents the interface the defines how the <see cref="LocationDialog"/> will query for locations.
    /// </summary>
    public interface IGeoSpatialService
    {
        Task<RouteDirections> GetRouteDirectionsAsync(double currentLatitude, double currentLongitude, double destinationLatitude, double destinationLongitude, string routeType = null);

        /// <summary>
        /// Gets the locations by a fuzzy search.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <param name="query">The location query.</param>
        /// <param name="country">The country code.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetLocationsByFuzzyQueryAsync(double latitude, double longitude, string query, string country);

        /// <summary>
        /// Gets the locations by address.
        /// </summary>
        /// <param name="address">The address query.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetLocationsByQueryAsync(string address);

        /// <summary>
        /// Gets the locations by coordinates.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetLocationsByPointAsync(double latitude, double longitude);

        /// <summary>
        /// Gets the map image URL.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="index">The pin point index.</param>
        /// <returns>Image URL string.</returns>
        string GetPointOfInterestMapImageURL(PointOfInterestModel pointOfInterest, int? index = null);

        /// <summary>
        /// Gets the locations nearby.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetLocationsNearby(double latitude, double longitude);
    }
}