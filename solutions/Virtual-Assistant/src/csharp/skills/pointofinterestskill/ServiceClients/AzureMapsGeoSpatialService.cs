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
    private static readonly string ImageUrlByPoint = $"https://atlas.microsoft.com/map/static/png?api-version=1.0&layer=basic&style=main&zoom={{2}}&center={{1}},{{0}}&width=500&height=280";
    private static readonly string GetRouteDirections = $"https://atlas.microsoft.com/route/directions/json?&api-version=1.0&query={{0}}";
    private static readonly string GetRouteDirectionsWithRouteType = $"https://atlas.microsoft.com/route/directions/json?&api-version=1.0&query={{0}}&&routeType={{1}}";
    private static readonly string PinImageBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABkAAAAcCAYAAACUJBTQAAAABmJLR0QA/wD/AP+gvaeTAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAB3RJTUUH4gcMECoy1DRNWQAAAB1pVFh0Q29tbWVudAAAAAAAQ3JlYXRlZCB3aXRoIEdJTVBkLmUHAAACBElEQVRIx+3WzU4TYRTG8f/bmc5HYTq2UiASYoIhLIwu0Iwbw0W4YuE9eAcudevCeA1uJMStSxMTXzZqFS0ilGJbaEttqc5Mh868LgyEBfiRgKs++5Nfztk8RyilFOccHcDzvHMDpJS/EAD/ztMzBzJLiwCk+A8ZIkNkiAyRITJEzqx+j7fY8fxLW540fxjxu0fC87y/gjJLi0gpT9+k+OiuMNWAbMZGWFn6mQLOxDS9IEJKqf4EHQGNCvVvLRH2exQcm/ZaCXvURStcJmXnxxi9NEO1O+DDRhXNyuCHAQdhn9LyYyGlPPUUh0BUfEXt8zthdGvY3Sp6p0oS9SiMX0RPJaRq9Qob21totok7lmO7ssaB3yGb0YiCFtNCnAgdAW+fYzixcNIR+E20oEl5dYVBr0XUaWAlPvrCrevsNBuE/QFpwyII+qy/f0lat9CyaZQBB52H4vjpDoFxQ4hnTxaZdKaYvbEA7gUIEjq7mxRcl53SG0wnh766ts5oziVOmTgjWQT73PbmCYKAdtImFtCLO8zlhWi2lfI8Dyklvf37QtPg6myB8mqF1y+WCcMITflM5vO4VoJjWOw2v6JH5gTVbgKxTq3VZvCjRXakwZWZaSwzDRrEyiVWcO+aQBYVpilEufyABPj08Qvzczf57ofs7e1hGzb1rU3261tMTU6RStv8BGcD7rXwL3cpAAAAAElFTkSuQmCC";
    private readonly string apiKey;
    private readonly string userLocale;

    public AzureMapsGeoSpatialService(string key, string locale = "en")
    {
        apiKey = key;
        userLocale = locale;
    }

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

    public async Task<LocationSet> GetLocationsByFuzzyQueryAsync(double latitude, double longitude, string query, string country)
    {
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (string.IsNullOrEmpty(country))
        {
            throw new ArgumentNullException(nameof(country));
        }

        var url = string.Format(CultureInfo.InvariantCulture, FindByFuzzyQueryApiUrl, latitude, longitude, query, country);
        return await GetLocationsAsync(url + "&subscription-key=" + apiKey);
    }

    public async Task<LocationSet> GetLocationsByQueryAsync(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            throw new ArgumentNullException(nameof(address));
        }

        return await GetLocationsAsync(FindByQueryApiUrl + Uri.EscapeDataString(address) + "&subscription-key=" + apiKey);
    }

    public async Task<LocationSet> GetLocationsByPointAsync(double latitude, double longitude)
    {
    return await GetLocationsAsync(
        string.Format(CultureInfo.InvariantCulture, FindByPointUrl, latitude, longitude) + "&subscription-key=" + apiKey);
    }

    public async Task<LocationSet> GetLocationsNearby(double latitude, double longitude)
    {
        return await GetLocationsAsync(
            string.Format(CultureInfo.InvariantCulture, FindNearbyUrl, latitude, longitude) + "&subscription-key=" + apiKey);
    }

    public string GetLocationMapImageUrl(Location location, int? index = null)
    {
        if (location == null)
        {
            throw new ArgumentNullException(nameof(location));
        }

        var point = location.Point;

        if (point == null)
        {
            throw new ArgumentNullException(nameof(point));
        }

        int zoom = 15;

        if (location.BoundaryBox != null && location.BoundaryBox.Count >= 4)
        {
            LatLng center;
            CalculateMapView(location.BoundaryBox, 500, 280, 0, out center, out zoom);
            point.Coordinates = new List<double>()
        {
            center.Latitude, center.Longitude,
        };
        }

        string imageUrl = string.Format(
            CultureInfo.InvariantCulture,
            ImageUrlByPoint,
            point.Coordinates[0],
            point.Coordinates[1],
            zoom) + "&subscription-key=" + apiKey;
        return imageUrl;
    }

    public string ImageToBase64(Image image, System.Drawing.Imaging.ImageFormat format)
    {
        using (var ms = new MemoryStream())
        {
            // Convert Image to byte[]
            image.Save(ms, format);
            byte[] imageBytes = ms.ToArray();

            // Convert byte[] to Base64 String
            string base64String = Convert.ToBase64String(imageBytes);
            return "data:image/png;base64," + base64String;
        }
    }

    public Image Base64ToImage(string base64String)
    {
        // Convert Base64 String to byte[]
        byte[] imageBytes = Convert.FromBase64String(base64String.Replace("data:image/png;base64,", string.Empty));
        using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
        {
            // Convert byte[] to Image
            ms.Write(imageBytes, 0, imageBytes.Length);
            Image image = Image.FromStream(ms, true);
            return image;
        }
    }

    private string GetMapImage(string mapImageUrl, string text)
        {
            HttpClient client = new HttpClient();
            using (var r = client.GetStreamAsync(mapImageUrl))
            {
                var bmp = System.Drawing.Bitmap.FromStream(r.Result);
                using (var g = Graphics.FromImage(bmp))
                {
                    var pin = Base64ToImage(PinImageBase64);
                    g.DrawImage(pin, 250 - 12, 140 - 28);
                    g.DrawString(text, new System.Drawing.Font("Arial", 12, FontStyle.Bold), Brushes.White, 250 - 3, 140 - 22);
                }

                return ImageToBase64(bmp, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

    private async Task<LocationSet> GetLocationsAsync(string url)
        {
            using (var client = new HttpClient())
            {
                url = url + $"&language={userLocale}";

                var response = await client.GetStringAsync(url);

                var apiResponse = JsonConvert.DeserializeObject<SearchResultSet>(response);

                var results = new LocationSet();

                if (apiResponse != null && apiResponse.Results != null)
                {
                    results.EstimatedTotal = apiResponse.Results.Count;
                    results.Locations = new List<Location>();
                    foreach (var r in apiResponse.Results)
                    {
                        results.Locations.Add(r.ToLocation());
                    }
                }

                return results;
            }
        }

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
        /// Calculates the best map view for a bounding box on the map.
        /// </summary>
    private void CalculateMapView(List<double> boundaryBox, double mapWidth, double mapHeight, int buffer, out LatLng center, out int zoom)
        {
            if (boundaryBox == null || boundaryBox.Count < 4)
            {
                center = new LatLng() { Latitude = 0, Longitude = 0 };
                zoom = 1;
                return;
            }

            center = new LatLng();
            double maxLat = boundaryBox[2];
            double minLat = boundaryBox[0];
            double maxLon = boundaryBox[3];
            double minLon = boundaryBox[1];
            center.Latitude = (maxLat + minLat) / 2;
            center.Longitude = (maxLon + minLon) / 2;
            double zoom1 = 1, zoom2 = 1;

            // Determine the best zoom level based on the map scale and bounding coordinate information
            if (maxLon != minLon && maxLat != minLat)
            {
                // Best zoom level based on map width
                zoom1 = Math.Log(360.0 / 256.0 * (mapWidth - (2 * buffer)) / (maxLon - minLon)) / Math.Log(2);

                // Best zoom level based on map height
                zoom2 = Math.Log(180.0 / 256.0 * (mapHeight - (2 * buffer)) / (maxLat - minLat)) / Math.Log(2);
            }

            // use the most zoomed out of the two zoom levels
            zoom = (int)Math.Floor((zoom1 < zoom2) ? zoom1 : zoom2);
        }
    }
}