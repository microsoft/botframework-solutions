// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Models.Foursquare;

namespace PointOfInterestSkill.Services
{
    public sealed class FoursquareGeoSpatialService : IGeoSpatialService
    {
        private static readonly string SearchForVenuesUrl = $"https://api.foursquare.com/v2/venues/search?ll={{0}},{{1}}&query={{2}}&radius={{3}}&intent=browse&limit={{4}}";
        private static readonly string SearchForVenuesByCategoryUrl = $"https://api.foursquare.com/v2/venues/search?categoryId={{2}}&ll={{0}},{{1}}&radius={{3}}&intent=browse&limit={{4}}";
        private static readonly string ExploreNearbyVenuesUrl = $"https://api.foursquare.com/v2/venues/explore?ll={{0}},{{1}}&radius={{2}}&limit={{3}}";
        private static readonly string ExploreVenuesUrl = $"https://api.foursquare.com/v2/venues/explore?ll={{0}},{{1}}&query={{2}}&radius={{3}}&limit={{4}}&sortByDistance=1";

        private static readonly string GetVenueDetailsUrl = $"https://api.foursquare.com/v2/venues/{{0}}?";

        /// <summary>
        /// Versioning is controlled by the v parameter, which is a date that represents the “version” of the API for which you expect from Foursquare.
        /// </summary>
        private readonly string apiVersion = "20190123";

        private string userLocale;
        private string clientId;
        private string clientSecret;
        private HttpClient httpClient;

        /// <summary>
        /// The maximum radius value for Foursquare is 100,000 meters.
        /// </summary>
        private int radius;

        /// <summary>
        /// The maxium limit of points of interest for Forsquare is 50.
        /// </summary>
        private int limit;

        public Task<IGeoSpatialService> InitClientAsync(string id, string secret, int radiusConfiguration, int limitConfiguration, string locale = "en-us", HttpClient client = null)
        {
            try
            {
                clientId = id;
                clientSecret = secret;
                userLocale = locale;
                radius = radiusConfiguration;
                limit = limitConfiguration;

                if (client == null)
                {
                    httpClient = ServiceHelper.GetHttpClient();
                }
                else
                {
                    httpClient = client;
                }
            }
            catch (Exception)
            {
            }

            return Task.FromResult(this as IGeoSpatialService);
        }

        public Task<IGeoSpatialService> InitKeyAsync(string key, int radiusConfiguration, int limitConfiguration, string locale = "en-us", HttpClient client = null)
        {
            throw new NotSupportedException();
        }

        public Task<RouteDirections> GetRouteDirectionsToDestinationAsync(double currentLatitude, double currentLongitude, double destinationLatitude, double destinationLongitude, string routeType = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a list of venues near the provided coordinates, matching a search term.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <param name="query">The search query.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public async Task<List<PointOfInterestModel>> GetPointOfInterestListByQueryAsync(double latitude, double longitude, string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            return await GetVenueAsync(string.Format(CultureInfo.InvariantCulture, ExploreVenuesUrl, latitude, longitude, query, radius, limit));
        }

        /// <summary>
        /// This provider does not offer search by address.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <param name="address">The search address.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public Task<List<PointOfInterestModel>> GetPointOfInterestListByAddressAsync(double latitude, double longitude, string address)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This provider does not offer search by only coordinates.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public Task<List<PointOfInterestModel>> GetPointOfInterestByCoordinatesAsync(double latitude, double longitude)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get venue recommendations using the latitude and longitude of the user's location.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public async Task<List<PointOfInterestModel>> GetNearbyPointOfInterestListAsync(double latitude, double longitude)
        {
            return await GetVenueAsync(
                string.Format(CultureInfo.InvariantCulture, ExploreNearbyVenuesUrl, latitude, longitude, radius, limit));
        }

        /// <summary>
        /// Get Point of Interest parking results around a specific location.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public async Task<List<PointOfInterestModel>> GetPointOfInterestListByParkingCategoryAsync(double latitude, double longitude)
        {
            // Available categories described at https://developer.foursquare.com/docs/resources/categories
            var parkingCategory = "4c38df4de52ce0d596b336e1";

            return await GetVenueAsync(
                string.Format(CultureInfo.InvariantCulture, SearchForVenuesByCategoryUrl, latitude, longitude, parkingCategory, radius, limit));
        }

        /// <summary>
        /// Returns available image from point of interest.
        /// </summary>
        /// <param name="pointOfInterest">The point of interest model.</param>
        /// <returns>PointOfInterestModel.</returns>
        public async Task<PointOfInterestModel> GetPointOfInterestDetailsAsync(PointOfInterestModel pointOfInterest)
        {
            if (pointOfInterest == null)
            {
                throw new ArgumentNullException(nameof(pointOfInterest));
            }

            var pointOfInterestList = await GetVenueAsync(
                string.Format(CultureInfo.InvariantCulture, GetVenueDetailsUrl, pointOfInterest.Id));

            return pointOfInterestList.FirstOrDefault() ?? pointOfInterest;
        }

        /// <summary>
        /// Gets a request to Foursquare API & convert to PointOfInterestModels.
        /// </summary>
        /// <param name="url">The HTTP request URL.</param>
        /// <returns>A list of PointOfInterestModels.</returns>
        private async Task<List<PointOfInterestModel>> GetVenueAsync(string url)
        {
            url = string.Concat(url, $"&client_id={clientId}&client_secret={clientSecret}&v={apiVersion}");

			try
			{
				var response = await httpClient.GetStringAsync(url);

				var apiResponse = JsonConvert.DeserializeObject<VenueResponse>(response);

				var pointOfInterestList = new List<PointOfInterestModel>();

				if (apiResponse?.Response != null)
				{
					if (apiResponse.Response.Venue != null)
					{
						var venue = apiResponse.Response.Venue;
						var newPointOfInterest = new PointOfInterestModel(venue);
						pointOfInterestList.Add(newPointOfInterest);
					}
					else if (apiResponse?.Response?.Venues != null)
					{
						foreach (var venue in apiResponse.Response.Venues)
						{
							var newPointOfInterest = new PointOfInterestModel(venue);
							pointOfInterestList.Add(newPointOfInterest);
						}
					}
					else if (apiResponse?.Response?.Groups != null)
					{
						foreach (var item in apiResponse.Response.Groups.First().Items)
						{
							var newPointOfInterest = new PointOfInterestModel(item.Venue);
							pointOfInterestList.Add(newPointOfInterest);
						}
					}
				}

				return pointOfInterestList;
			}
			catch (Exception ex)
			{
				throw new Exception($"{ex.Message}. failed URL: {url}", ex);
			}
        }
    }
}