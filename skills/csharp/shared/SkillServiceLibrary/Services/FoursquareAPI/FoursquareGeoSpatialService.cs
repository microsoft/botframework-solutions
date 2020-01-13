// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SkillServiceLibrary.Models;
using SkillServiceLibrary.Models.Foursquare;
using SkillServiceLibrary.Services;
using SkillServiceLibrary.Utilities;

namespace SkillServiceLibrary.Services.FoursquareAPI
{
    public sealed class FoursquareGeoSpatialService : IGeoSpatialService
    {
        public static readonly string ProviderName = "Foursquare";
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

        public string Provider { get { return ProviderName; } }

        public Task<IGeoSpatialService> InitClientAsync(string id, string secret, int radiusConfiguration, int limitConfiguration, int routeLimitConfiguration, string locale = "en-us", HttpClient client = null)
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

        public Task<IGeoSpatialService> InitKeyAsync(string key, int radiusConfiguration, int limitConfiguration, int routeLimitConfiguration, string locale = "en-us", HttpClient client = null)
        {
            throw new NotSupportedException();
        }

        public Task<RouteDirections> GetRouteDirectionsToDestinationAsync(double currentLatitude, double currentLongitude, double destinationLatitude, double destinationLongitude, string routeType = null)
        {
            throw new NotSupportedException();
        }

        public Task<string> GetRouteImageAsync(PointOfInterestModel destination, RouteDirections.Route route, int width = 0, int height = 0)
        {
            throw new NotSupportedException();
        }

        public Task<string> GetAllPointOfInterestsImageAsync(LatLng currentCoordinates, List<PointOfInterestModel> pointOfInterestModels, int width = 0, int height = 0)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns a list of venues near the provided coordinates, matching a search term.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <param name="query">The search query.</param>
        /// <param name="poiType">The poi type.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public async Task<List<PointOfInterestModel>> GetPointOfInterestListByQueryAsync(double latitude, double longitude, string query, string poiType = null)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            return await GetVenueAsync(string.Format(CultureInfo.InvariantCulture, SearchForVenuesUrl, latitude, longitude, query, radius, limit), poiType);
        }

        public async Task<List<PointOfInterestModel>> GetPointOfInterestListByCategoryAsync(double latitude, double longitude, string category, string poiType = null, bool unique = false)
        {
            if (string.IsNullOrEmpty(category))
            {
                throw new ArgumentNullException(nameof(category));
            }

            var searchLimit = unique ? limit * 2 : limit;

            var result = await GetVenueAsync(string.Format(CultureInfo.InvariantCulture, ExploreVenuesUrl, latitude, longitude, category, radius, searchLimit), poiType);

            if (unique)
            {
                // preserve original order
                var uniqueResult = new List<PointOfInterestModel>();
                var uniqueNames = new HashSet<string>();
                foreach (var model in result)
                {
                    if (!uniqueNames.Contains(model.Name))
                    {
                        uniqueResult.Add(model);
                        uniqueNames.Add(model.Name);
                        if (uniqueResult.Count >= limit)
                        {
                            break;
                        }
                    }
                }

                result = uniqueResult;
            }

            return result;
        }

        /// <summary>
        /// This provider does not offer search by address.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <param name="address">The search address.</param>
        /// <param name="poiType">The poi type.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public Task<List<PointOfInterestModel>> GetPointOfInterestListByAddressAsync(double latitude, double longitude, string address, string poiType = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This provider does not offer search by only coordinates.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <param name="poiType">The poi type.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public Task<List<PointOfInterestModel>> GetPointOfInterestByCoordinatesAsync(double latitude, double longitude, string poiType = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get venue recommendations using the latitude and longitude of the user's location.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <param name="poiType">The poi type.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public async Task<List<PointOfInterestModel>> GetNearbyPointOfInterestListAsync(double latitude, double longitude, string poiType = null)
        {
            return await GetVenueAsync(
                string.Format(CultureInfo.InvariantCulture, ExploreNearbyVenuesUrl, latitude, longitude, radius, limit), poiType);
        }

        /// <summary>
        /// Get Point of Interest parking results around a specific location.
        /// </summary>
        /// <param name="latitude">The current latitude.</param>
        /// <param name="longitude">The current longitude.</param>
        /// <param name="poiType">The poi type.</param>
        /// <returns>List of PointOfInterestModels.</returns>
        public async Task<List<PointOfInterestModel>> GetPointOfInterestListByParkingCategoryAsync(double latitude, double longitude, string poiType = null)
        {
            // Available categories described at https://developer.foursquare.com/docs/resources/categories
            var parkingCategory = "4c38df4de52ce0d596b336e1";

            return await GetVenueAsync(
                string.Format(CultureInfo.InvariantCulture, SearchForVenuesByCategoryUrl, latitude, longitude, parkingCategory, radius, limit), poiType);
        }

        /// <summary>
        /// Returns available image from point of interest.
        /// </summary>
        /// <param name="pointOfInterest">The point of interest model.</param>
        /// <param name="width">The image width.</param>
        /// <param name="height">The image height.</param>
        /// <returns>PointOfInterestModel.</returns>
        public async Task<PointOfInterestModel> GetPointOfInterestDetailsAsync(PointOfInterestModel pointOfInterest, int width = 0, int height = 0)
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
        /// <param name="poiType">The poi type.</param>
        /// <returns>A list of PointOfInterestModels.</returns>
        private async Task<List<PointOfInterestModel>> GetVenueAsync(string url, string poiType = null)
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
                        ImageToDataUri(newPointOfInterest);
                        pointOfInterestList.Add(newPointOfInterest);
                    }
                    else if (apiResponse?.Response?.Venues != null)
                    {
                        if (!string.IsNullOrEmpty(poiType))
                        {
                            if (poiType == GeoSpatialServiceTypes.PoiType.Nearest)
                            {
                                var nearestResult = apiResponse.Response.Venues.Aggregate((agg, next) => agg.Location.Distance <= next.Location.Distance ? agg : next);

                                if (nearestResult != null)
                                {
                                    apiResponse.Response.Venues = new Venue[] { nearestResult };
                                }
                            }
                        }

                        foreach (var venue in apiResponse.Response.Venues)
                        {
                            var newPointOfInterest = new PointOfInterestModel(venue);
                            ImageToDataUri(newPointOfInterest);
                            pointOfInterestList.Add(newPointOfInterest);
                        }
                    }
                    else if (apiResponse?.Response?.Groups != null)
                    {
                        if (!string.IsNullOrEmpty(poiType))
                        {
                            if (poiType == GeoSpatialServiceTypes.PoiType.Nearest)
                            {
                                var nearestResult = apiResponse.Response.Groups.First().Items.Aggregate((agg, next) => agg.Venue.Location.Distance <= next.Venue.Location.Distance ? agg : next);

                                if (nearestResult != null)
                                {
                                    apiResponse.Response.Groups.First().Items = new Item[] { nearestResult };
                                }
                            }
                        }

                        foreach (var item in apiResponse.Response.Groups.First().Items)
                        {
                            var newPointOfInterest = new PointOfInterestModel(item.Venue);
                            ImageToDataUri(newPointOfInterest);
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

        private void ImageToDataUri(PointOfInterestModel model)
        {
            // TODO set this > 0 (like 75) to convert url links of images to jpeg data uris (e.g. when need transcripts)
            long useDataUriJpegQuality = 0L;

            if (useDataUriJpegQuality > 0 && !string.IsNullOrEmpty(model.PointOfInterestImageUrl))
            {
                using (var image = Image.FromStream(httpClient.GetStreamAsync(model.PointOfInterestImageUrl).Result))
                {
                    MemoryStream ms = new MemoryStream();
                    var encoder = ImageCodecInfo.GetImageDecoders().Where(x => x.FormatID == ImageFormat.Jpeg.Guid).FirstOrDefault();
                    var encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, useDataUriJpegQuality);
                    image.Save(ms, encoder, encoderParameters);
                    model.PointOfInterestImageUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(ms.ToArray())}";
                }
            }
        }
    }
}