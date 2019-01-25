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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentLatitude"></param>
        /// <param name="currentLongitude"></param>
        /// <param name="destinationLatitude"></param>
        /// <param name="destinationLongitude"></param>
        /// <param name="routeType"></param>
        /// <returns></returns>
        Task<RouteDirections> GetRouteDirectionsAsync(double currentLatitude, double currentLongitude, double destinationLatitude, double destinationLongitude, string routeType = null);

        /// <summary>
        /// Gets the points of interest by a fuzzy search.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <param name="query">The location query.</param>
        /// <param name="country">The country code.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestByQueryAsync(double latitude, double longitude, string query, string country);

        /// <summary>
        /// Gets the point of interest by address.
        /// </summary>
        /// <param name="address">The address query.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestByAddressAsync(string address);

        /// <summary>
        /// Gets the point of interest by coordinates.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestByPointAsync(double latitude, double longitude);

        /// <summary>
        /// Gets point of interest details.
        /// </summary>
        /// <param name="pointOfInterest">The point of interest.</param>
        /// <returns>Image URL string.</returns>
        Task<PointOfInterestModel> GetPointOfInterestDetails(PointOfInterestModel pointOfInterest);

        /// <summary>
        /// Gets the points of interest nearby.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetLocationsNearby(double latitude, double longitude);
    }
}