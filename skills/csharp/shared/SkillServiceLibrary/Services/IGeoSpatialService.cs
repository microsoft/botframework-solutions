// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using SkillServiceLibrary.Models;

namespace SkillServiceLibrary.Services
{
    /// <summary>
    /// Represents the interface the defines how skill will query for points of interest.
    /// </summary>
    public interface IGeoSpatialService
    {
        string Provider { get; }

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
        /// Get image for route to destination.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="route">The route.</param>
        /// <param name="width">Image width. 0 for default.</param>
        /// <param name="height">Image height. 0 for default.</param>
        /// <returns>The image url.</returns>
        Task<string> GetRouteImageAsync(PointOfInterestModel destination, RouteDirections.Route route, int width = 0, int height = 0);

        /// <summary>
        /// Get an image containing all point of interests.
        /// </summary>
        /// <param name="currentCoordinates">Current location. Could be null.</param>
        /// <param name="pointOfInterestModels">Point of interests.</param>
        /// <param name="width">Image width. 0 for default.</param>
        /// <param name="height">Image height. 0 for default.</param>
        /// <returns>The image url.</returns>
        Task<string> GetAllPointOfInterestsImageAsync(LatLng currentCoordinates, List<PointOfInterestModel> pointOfInterestModels, int width = 0, int height = 0);

        /// <summary>
        /// Gets the points of interest by a fuzzy search.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <param name="query">The location query.</param>
        /// <param name="poiType">The poi type.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestListByQueryAsync(double latitude, double longitude, string query, string poiType = null);

        /// <summary>
        /// Gets the points of interest by a fuzzy search.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <param name="category">The category query.</param>
        /// <param name="poiType">The poi type.</param>
        /// <param name="unique">Wether return unique results for each brand in the category.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestListByCategoryAsync(double latitude, double longitude, string category, string poiType = null, bool unique = false);

        /// <summary>
        /// Gets the point of interest by address.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <param name="address">The address query.</param>
        /// <param name="poiType">The poi type.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestListByAddressAsync(double latitude, double longitude, string address, string poiType = null);

        /// <summary>
        /// Gets the point of interest by coordinates.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <param name="poiType">The poi type.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestByCoordinatesAsync(double latitude, double longitude, string poiType = null);

        /// <summary>
        /// Gets the point of interest by parking category.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <param name="poiType">The poi type.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetPointOfInterestListByParkingCategoryAsync(double latitude, double longitude, string poiType = null);

        /// <summary>
        /// Gets point of interest details.
        /// </summary>
        /// <param name="pointOfInterest">The point of interest.</param>
        /// <param name="width">Image width. 0 for default.</param>
        /// <param name="height">Image height. 0 for default.</param>
        /// <returns>Image URL string.</returns>
        Task<PointOfInterestModel> GetPointOfInterestDetailsAsync(PointOfInterestModel pointOfInterest, int width = 0, int height = 0);

        /// <summary>
        /// Gets the points of interest nearby.
        /// </summary>
        /// <param name="latitude">The point latitude.</param>
        /// <param name="longitude">The point longitude.</param>
        /// <param name="poiType">The poi type.</param>
        /// <returns>The found locations.</returns>
        Task<List<PointOfInterestModel>> GetNearbyPointOfInterestListAsync(double latitude, double longitude, string poiType = null);

        /// <summary>
        /// Init task service.
        /// </summary>
        /// <param name="key">Geospatial service key.</param>
        /// <param name="radiusConfiguration">The radius from configuration.</param>
        /// <param name="limitConfiguration">The limit size from configuration.</param>
        /// <param name="routeLimitConfiguration">The limit size of route.</param>
        /// <param name="locale">The user locale.</param>
        /// <param name="client">the httpclient for making the API request.</param>
        /// <returns>Task service itself.</returns>
        Task<IGeoSpatialService> InitKeyAsync(string key, int radiusConfiguration, int limitConfiguration, int routeLimitConfiguration, string locale = "en", HttpClient client = null);

        /// <summary>
        /// Init task service.
        /// </summary>
        /// <param name="clientId">Geospatial service client id.</param>
        /// <param name="clientSecret">Geospatial service client secret.</param>
        /// <param name="radiusConfiguration">The radius from configuration.</param>
        /// <param name="limitConfiguration">The limit size from configuration.</param>
        /// <param name="routeLimitConfiguration">The limit size of route.</param>
        /// <param name="locale">The user locale.</param>
        /// <param name="client">the httpclient for making the API request.</param>
        /// <returns>Task service itself.</returns>
        Task<IGeoSpatialService> InitClientAsync(string clientId, string clientSecret, int radiusConfiguration, int limitConfiguration, int routeLimitConfiguration, string locale = "en", HttpClient client = null);
    }
}
