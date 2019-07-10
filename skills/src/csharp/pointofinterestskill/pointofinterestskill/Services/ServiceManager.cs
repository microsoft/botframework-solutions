﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;

namespace PointOfInterestSkill.Services
{
    public class ServiceManager : IServiceManager
    {
        private int radiusInt = 25000;
        private int limitSizeInt = 3;

        public IGeoSpatialService InitMapsService(BotSettings settings, string locale = "en")
        {
            var clientId = settings.FoursquareClientId;
            var clientSecret = settings.FoursquareClientSecret;
            var radius = settings.Radius;
            var limitSize = settings.LimitSize;

            var clientIdStr = clientId;
            var clientSecretStr = clientSecret;
            radiusInt = (radius != null) ? Convert.ToInt32(radius) : radiusInt;
            limitSizeInt = (limitSize != null) ? Convert.ToInt32(limitSize) : limitSizeInt;

            if (!string.IsNullOrEmpty(clientIdStr) && !string.IsNullOrEmpty(clientSecretStr))
            {
                return new FoursquareGeoSpatialService().InitClientAsync(clientIdStr, clientSecretStr, radiusInt, limitSizeInt, locale).Result;
            }
            else
            {
                var key = GetAzureMapsKey(settings);

                return new AzureMapsGeoSpatialService().InitKeyAsync(key, radiusInt, limitSizeInt, locale).Result;
            }
        }

        /// <summary>
        /// Gets the supported GeoSpatialService for route directions.
        /// Azure Maps is the only supported provider.
        /// </summary>
        /// <param name="services">The SkillConfigurationBase services.</param>
        /// <param name="locale">The user's locale.</param>
        /// <returns>IGeoSpatialService.</returns>
        public IGeoSpatialService InitRoutingMapsService(BotSettings settings, string locale = "en")
        {
            var radius = settings.Radius;
            var limitSize = settings.LimitSize;
            radiusInt = (radius != null) ? Convert.ToInt32(radius) : radiusInt;
            limitSizeInt = (limitSize != null) ? Convert.ToInt32(limitSize) : limitSizeInt;

            var key = GetAzureMapsKey(settings);

            return new AzureMapsGeoSpatialService().InitKeyAsync(key, radiusInt, limitSizeInt, locale).Result;
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
            var radius = settings.Radius;
            radiusInt = (radius != null) ? Convert.ToInt32(radius) : radiusInt;

            var key = GetAzureMapsKey(settings);

            return new AzureMapsGeoSpatialService().InitKeyAsync(key, radiusInt, limitSizeInt, locale).Result;
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
    }
}