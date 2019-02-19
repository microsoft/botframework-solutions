// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.Bot.Solutions.Skills;

namespace PointOfInterestSkill.ServiceClients
{
    public class ServiceManager : IServiceManager
    {
        private int radiusInt = 25000;
        private int limitSizeInt = 3;

        public IGeoSpatialService InitMapsService(SkillConfigurationBase services, string locale = "en")
        {
            services.Properties.TryGetValue("FoursquareClientId", out var clientId);
            services.Properties.TryGetValue("FoursquareClientSecret", out var clientSecret);
            services.Properties.TryGetValue("Radius", out var radius);
            services.Properties.TryGetValue("LimitSize", out var limitSize);

            var clientIdStr = (string)clientId;
            var clientSecretStr = (string)clientSecret;
            radiusInt = (radius != null) ? Convert.ToInt32(radius) : radiusInt;
            limitSizeInt = (limitSize != null) ? Convert.ToInt32(limitSize) : limitSizeInt;

            if (!string.IsNullOrEmpty(clientIdStr) && !string.IsNullOrEmpty(clientSecretStr))
            {
                return new FoursquareGeoSpatialService().InitClientAsync(clientIdStr, clientSecretStr, radiusInt, limitSizeInt, locale).Result;
            }
            else
            {
                var key = GetAzureMapsKey(services);

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
        public IGeoSpatialService InitRoutingMapsService(SkillConfigurationBase services, string locale = "en")
        {
            services.Properties.TryGetValue("Radius", out var radius);
            services.Properties.TryGetValue("LimitSize", out var limitSize);
            radiusInt = (radius != null) ? Convert.ToInt32(radius) : radiusInt;
            limitSizeInt = (limitSize != null) ? Convert.ToInt32(limitSize) : limitSizeInt;

            var key = GetAzureMapsKey(services);

            return new AzureMapsGeoSpatialService().InitKeyAsync(key, radiusInt, limitSizeInt, locale).Result;
        }

        /// <summary>
        /// Gets the supported GeoSpatialService for reverse address search.
        /// Azure Maps is the only supported provider.
        /// </summary>
        /// <param name="services">The SkillConfigurationBase services.</param>
        /// <param name="locale">The user's locale.</param>
        /// <returns>IGeoSpatialService.</returns>
        public IGeoSpatialService InitAddressMapsService(SkillConfigurationBase services, string locale = "en-us")
        {
            services.Properties.TryGetValue("Radius", out var radius);
            radiusInt = (radius != null) ? Convert.ToInt32(radius) : radiusInt;

            var key = GetAzureMapsKey(services);

            return new AzureMapsGeoSpatialService().InitKeyAsync(key, radiusInt, limitSizeInt, locale).Result;
        }

        /// <summary>
        /// Gets Azure Maps key from the skill configuration.
        /// </summary>
        /// <param name="services">The skill configuration object.</param>
        /// <returns>Azure Maps key string.</returns>
        protected string GetAzureMapsKey(SkillConfigurationBase services)
        {
            services.Properties.TryGetValue("AzureMapsKey", out var key);

            var keyStr = (string)key;
            if (string.IsNullOrWhiteSpace(keyStr))
            {
                throw new Exception("Could not get the required Azure Maps key. Please make sure your settings are correctly configured.");
            }
            else
            {
                return keyStr;
            }
        }
    }
}