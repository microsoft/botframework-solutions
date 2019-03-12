// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using PointOfInterestSkill.Models;

namespace PointOfInterestSkill.ServiceClients
{
    /// <summary>
    /// Represents the interface the defines how the <see cref="PointOfInterestSKillDialog"/> will query for points of interest.
    /// </summary>
    public interface IGeoSpatialService
    {
        /// <summary>
        /// Gets route directions from origin to destination.
        /// </summary>
        /// <param name="currentLatitude">The origin lat.</param>
        /// <param name="currentLongitude">The origin lon.</param>
        /// <param name="destinationLatitude">The destination's lat.</param>
        /// <param name="destinationLongitude">The destination's lon.</param>
        /// <param name="routeType">The route type.</param>
        /// <returns>Route directions.</returns>
        Task<RouteDirections> GetRouteDirectionsToDestinationAsync(double currentLatitude, double currentLongitude, double destinationLatitude, double destinationLongitude, string routeType = null);

        /// <summary>
        /// Gets the points of interest by a fuzzy search.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <param name="query">The location query.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestListByQueryAsync(double latitude, double longitude, string query);

        /// <summary>
        /// Gets the point of interest by address.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <param name="address">The address query.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestListByAddressAsync(double latitude, double longitude, string address);

        /// <summary>
        /// Gets the point of interest by coordinates.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestByCoordinatesAsync(double latitude, double longitude);

        /// <summary>
        /// Gets the point of interest by parking category.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestListByParkingCategoryAsync(double latitude, double longitude);

        /// <summary>
        /// Gets point of interest details.
        /// </summary>
        /// <param name="pointOfInterest">The point of interest.</param>
        /// <returns>Image URL string.</returns>
        Task<PointOfInterestModel> GetPointOfInterestDetailsAsync(PointOfInterestModel pointOfInterest);

        /// <summary>
        /// Gets the points of interest nearby.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetNearbyPointOfInterestListAsync(double latitude, double longitude);

        /// <summary>
        /// Init task service.
        /// </summary>
        /// <param name="key">Geospatial service key.</param>
        /// <param name="radiusConfiguration">The radius from configuration.</param>
        /// <param name="limitConfiguration">The limit size from configuration.</param>
        /// <param name="locale">The user locale.</param>
        /// <param name="client">the httpclient for making the API request.</param>
        /// <returns>Task service itself.</returns>
        Task<IGeoSpatialService> InitKeyAsync(string key, int radiusConfiguration, int limitConfiguration, string locale = "en", HttpClient client = null);

        /// <summary>
        /// Init task service.
        /// </summary>
        /// <param name="clientId">Geospatial service client id.</param>
        /// <param name="clientSecret">Geospatial service client secret.</param>
        /// <param name="radiusConfiguration">The radius from configuration.</param>
        /// <param name="limitConfiguration">The limit size from configuration.</param>
        /// <param name="locale">The user locale.</param>
        /// <param name="client">the httpclient for making the API request.</param>
        /// <returns>Task service itself.</returns>
        Task<IGeoSpatialService> InitClientAsync(string clientId, string clientSecret, int radiusConfiguration, int limitConfiguration, string locale = "en", HttpClient client = null);
    }
}