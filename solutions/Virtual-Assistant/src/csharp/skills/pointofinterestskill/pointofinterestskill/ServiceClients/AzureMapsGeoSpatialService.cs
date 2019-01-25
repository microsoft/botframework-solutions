// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PointOfInterestSkill.Models;

namespace PointOfInterestSkill.ServiceClients
{
    public sealed class AzureMapsGeoSpatialService : IGeoSpatialService
    {
        private static readonly string FindByFuzzyQueryApiUrl = $"https://atlas.microsoft.com/search/fuzzy/json?api-version=1.0&limit=3&lat={{0}}&lon={{1}}&query={{2}}&countryset={{3}}";
        private static readonly string FindByQueryApiUrl = $"https://atlas.microsoft.com/search/address/json?api-version=1.0&limit=3&query=";
        private static readonly string FindByPointUrl = $"https://atlas.microsoft.com/search/address/reverse/json?api-version=1.0&query={{0}},{{1}}";
        private static readonly string FindNearbyUrl = $"https://atlas.microsoft.com/search/nearby/json?api-version=1.0&limit=3&lat={{0}}&lon={{1}}";
        private static readonly string ImageUrlByPoint = $"https://atlas.microsoft.com/map/static/png?api-version=1.0&layer=basic&style=main&zoom={{2}}&center={{1}},{{0}}&width=512&height=512";
        private static readonly string GetRouteDirections = $"https://atlas.microsoft.com/route/directions/json?&api-version=1.0&query={{0}}";
        private static readonly string GetRouteDirectionsWithRouteType = $"https://atlas.microsoft.com/route/directions/json?&api-version=1.0&query={{0}}&&routeType={{1}}";
        private readonly string apiKey;
        private readonly string userLocale;

        public AzureMapsGeoSpatialService(string key, string locale = "en")
        {
            apiKey = key;
            userLocale = locale;
        }

        /// <summary>
        /// Get points of interest weighted by coordinates and using a free for search query.
        /// </summary>
        public async Task<List<PointOfInterestModel>> GetPointOfInterestByQueryAsync(double latitude, double longitude, string query, string country)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (string.IsNullOrEmpty(country))
            {
                throw new ArgumentNullException(nameof(country));
            }

            return await GetPointsOfInterestAsync(string.Format(CultureInfo.InvariantCulture, FindByFuzzyQueryApiUrl, latitude, longitude, query, country));
        }

        /// <summary>
        /// Get coordinates from a street address.
        /// </summary>
        public async Task<List<PointOfInterestModel>> GetPointOfInterestByAddressAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentNullException(nameof(address));
            }

            return await GetPointsOfInterestAsync(FindByQueryApiUrl + Uri.EscapeDataString(address));
        }

        /// <summary>
        /// Get a street address from coordinates.
        /// </summary>
        public async Task<List<PointOfInterestModel>> GetPointOfInterestByPointAsync(double latitude, double longitude)
        {
        return await GetPointsOfInterestAsync(
            string.Format(CultureInfo.InvariantCulture, FindByPointUrl, latitude, longitude));
        }

        /// <summary>
        /// Get Point of Interest results around a specific location.
        /// </summary>
        public async Task<List<PointOfInterestModel>> GetLocationsNearby(double latitude, double longitude)
        {
            return await GetPointsOfInterestAsync(
                string.Format(CultureInfo.InvariantCulture, FindNearbyUrl, latitude, longitude));
        }

        /// <summary>
        /// Get a static map image URL of the Point of Interest and returns PointOfInterestModel.
        /// </summary>
        public async Task<PointOfInterestModel> GetPointOfInterestDetails(PointOfInterestModel pointOfInterest)
        {
            int zoom = 15;

            string imageUrl = string.Format(
                CultureInfo.InvariantCulture,
                ImageUrlByPoint,
                pointOfInterest?.Geolocation?.Latitude,
                pointOfInterest?.Geolocation?.Longitude,
                zoom) + "&subscription-key=" + apiKey;

            pointOfInterest.ImageUrl = imageUrl;

            return pointOfInterest;
        }

        /// <summary>
        /// Get Azure Maps route based on available parameters.
        /// </summary>
        public async Task<RouteDirections> GetRouteDirectionsAsync(double currentLatitude, double currentLongitude, double destinationLatitude, double destinationLongitude, string routeType = null)
        {
            if (string.IsNullOrEmpty(routeType))
            {
                return await GetRouteDirectionsAsync(string.Format(CultureInfo.InvariantCulture, GetRouteDirections, currentLatitude + "," + currentLongitude + ":" + destinationLatitude + "," + destinationLongitude) + "&subscription-key=" + apiKey);
            }
            else
            {
                return await GetRouteDirectionsAsync(string.Format(CultureInfo.InvariantCulture, GetRouteDirectionsWithRouteType, currentLatitude + "," + currentLongitude + ":" + destinationLatitude + "," + destinationLongitude, routeType) + "&subscription-key=" + apiKey);
            }
        }

        /// <summary>
        /// Get route directions response from Azure Maps.
        /// </summary>
        private async Task<RouteDirections> GetRouteDirectionsAsync(string url)
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);

                var apiResponse = JsonConvert.DeserializeObject<RouteDirections>(response);

                return apiResponse;
            }
        }

        /// <summary>
        /// Get search reuslts response from Azure Maps and convert to point of interest list.
        /// </summary>
        private async Task<List<PointOfInterestModel>> GetPointsOfInterestAsync(string url)
        {
            using (var client = new HttpClient())
            {
                url = url + $"&language={userLocale}&subscription-key={apiKey}";

                var response = await client.GetStringAsync(url);

                var apiResponse = JsonConvert.DeserializeObject<SearchResultSet>(response);

                var pointOfInterestList = new List<PointOfInterestModel>();

                if (apiResponse != null && apiResponse.Results != null)
                {
                    foreach (var searchResult in apiResponse.Results)
                    {
                        var newPointOfInterest = new PointOfInterestModel(searchResult);
                        pointOfInterestList.Add(newPointOfInterest);
                    }
                }

                return pointOfInterestList;
            }
        }
    }
}