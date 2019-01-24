// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Solutions.Skills;
using System;

namespace PointOfInterestSkill.ServiceClients
{
    public class ServiceManager : IServiceManager
    {
        public IGeoSpatialService InitMapsService(SkillConfiguration services, string locale = "en")
        {
            services.Properties.TryGetValue("FoursquareClientId", out var clientId);
            services.Properties.TryGetValue("FoursquareClientSecret", out var clientSecret);

            var clientIdStr = (string)clientId;
            var clientSecretStr = (string)clientSecret;

            if (clientIdStr != null && clientSecretStr != null)
            {
                return new FoursquareGeoSpatialService(clientIdStr, clientSecretStr);
            }
            else
            {
                var key = GetAzureMapsKey(services);

                return new AzureMapsGeoSpatialService(key, locale);
            }
        }

        /// <summary>
        /// Gets the supported GeoSpatialService for route directions.
        /// Azure Maps is the only supported provider.
        /// </summary>
        public IGeoSpatialService InitRoutingMapsService(SkillConfiguration services, string locale = "en")
        {
            var key = GetAzureMapsKey(services);

            return new AzureMapsGeoSpatialService(key, locale);
        }

        protected string GetAzureMapsKey(SkillConfiguration services)
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