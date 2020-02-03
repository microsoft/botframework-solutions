// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using SkillServiceLibrary.Services;
using SkillServiceLibrary.Services.AzureMapsAPI;
using SkillServiceLibrary.Services.FoursquareAPI;

namespace PointOfInterestSkill.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly int radiusInt = 25000;
        private readonly int limitSizeInt = 3;
        private readonly int routeLimitInt = 1;

        public IGeoSpatialService InitMapsService(BotSettings settings, string locale = "en")
        {
            (int radius, int limit, int routeLimit) = GetSettings(settings);

            var clientIdStr = settings.FoursquareClientId;
            var clientSecretStr = settings.FoursquareClientSecret;

            if (!string.IsNullOrEmpty(clientIdStr) && !string.IsNullOrEmpty(clientSecretStr))
            {
                return new FoursquareGeoSpatialService().InitClientAsync(clientIdStr, clientSecretStr, radius, limit, routeLimit, locale).Result;
            }
            else
            {
                var key = GetAzureMapsKey(settings);

                return new AzureMapsGeoSpatialService().InitKeyAsync(key, radius, limit, routeLimit, locale).Result;
            }
        }

        /// <summary>
        /// Gets the supported GeoSpatialService for route directions.
        /// Azure Maps is the only supported provider.
        /// </summary>
        /// <param name="settings">The BotSettings class.</param>
        /// <param name="locale">The user's locale.</param>
        /// <returns>IGeoSpatialService.</returns>
        public IGeoSpatialService InitRoutingMapsService(BotSettings settings, string locale = "en")
        {
            (int radius, int limit, int routeLimit) = GetSettings(settings);

            var key = GetAzureMapsKey(settings);

            return new AzureMapsGeoSpatialService().InitKeyAsync(key, radius, limit, routeLimit, locale).Result;
        }

        /// <summary>
        /// Gets the supported GeoSpatialService for reverse address search.
        /// Azure Maps is the only supported provider.
        /// </summary>
        /// <param name="settings">The BotSettings class.</param>
        /// <param name="locale">The user's locale.</param>
        /// <returns>IGeoSpatialService.</returns>
        public IGeoSpatialService InitAddressMapsService(BotSettings settings, string locale = "en-us")
        {
            (int radius, int limit, int routeLimit) = GetSettings(settings);

            var key = GetAzureMapsKey(settings);

            return new AzureMapsGeoSpatialService().InitKeyAsync(key, radius, limit, routeLimit, locale).Result;
        }

        /// <summary>
        /// Gets Azure Maps key from the skill configuration.
        /// </summary>
        /// <param name="settings">The skill configuration object.</param>
        /// <returns>Azure Maps key string.</returns>
        protected string GetAzureMapsKey(BotSettings settings)
        {
            var key = settings.AzureMapsKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new Exception("Could not get the required Azure Maps key. Please make sure your settings are correctly configured.");
            }
            else
            {
                return key;
            }
        }

        /// <summary>
        /// Return settings or default.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <returns>Radius, limit, route limit.</returns>
        private (int, int, int) GetSettings(BotSettings settings)
        {
            if (!int.TryParse(settings.Radius, out int radius))
            {
                radius = radiusInt;
            }

            if (!int.TryParse(settings.LimitSize, out int limit))
            {
                limit = limitSizeInt;
            }

            if (!int.TryParse(settings.RouteLimit, out int routeLimit))
            {
                routeLimit = routeLimitInt;
            }

            return (radius, limit, routeLimit);
        }
    }
}