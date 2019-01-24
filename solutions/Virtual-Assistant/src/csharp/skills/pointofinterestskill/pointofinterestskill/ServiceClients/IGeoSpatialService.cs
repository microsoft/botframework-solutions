// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

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
        /// Gets the locations asynchronously.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <param name="query">The location query.</param>
        /// <param name="country">The country code.</param>
        /// <returns>The found locations.</returns>
        Task<LocationSet> GetLocationsByFuzzyQueryAsync(double latitude, double longitude, string query, string country);

        /// <summary>
        /// Gets the locations asynchronously.
        /// </summary>
        /// <param name="address">The address query.</param>
        /// <returns>The found locations.</returns>
        Task<LocationSet> GetLocationsByQueryAsync(string address);

        /// <summary>
        /// Gets the locations asynchronously.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <returns>The found locations.</returns>
        Task<LocationSet> GetLocationsByPointAsync(double latitude, double longitude);

        /// <summary>
        /// Gets the map image URL.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="index">The pin point index.</param>
        /// <returns>Image URL string.</returns>
        string GetLocationMapImageUrl(Location location, int? index = null);

        /// <summary>
        /// Gets the locations asynchronously.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <returns>The found locations.</returns>
        Task<LocationSet> GetLocationsNearby(double latitude, double longitude);
    }
}