﻿using System;
using System.Net.Http;
using PointOfInterestSkill.Services;

namespace PointOfInterestSkillTests.API.Fakes
{
    public class MockServiceManager : IServiceManager
    {
        private HttpClient mockClient;

        public MockServiceManager()
        {
            mockClient = new HttpClient(new MockHttpClientHandlerGen().GetMockHttpClientHandler());
        }

        public IGeoSpatialService InitMapsService(BotSettings services, string locale = "en-us")
        {
            services.Properties.TryGetValue("FoursquareClientId", out var clientId);
            services.Properties.TryGetValue("FoursquareClientSecret", out var clientSecret);

            var clientIdStr = (string)clientId;
            var clientSecretStr = (string)clientSecret;

            if (clientIdStr != null && clientSecretStr != null)
            {
                return new FoursquareGeoSpatialService().InitClientAsync(clientIdStr, clientSecretStr, MockData.Radius, MockData.Limit, MockData.Locale, mockClient).Result;
            }
            else
            {
                var key = GetAzureMapsKey(services);

                return new AzureMapsGeoSpatialService().InitKeyAsync(key, MockData.Radius, MockData.Limit, locale, mockClient).Result;
            }
        }

        public IGeoSpatialService InitAddressMapsService(BotSettings services, string locale = "en-us")
        {
            var key = GetAzureMapsKey(services);

            return new AzureMapsGeoSpatialService().InitKeyAsync(key, MockData.Radius, MockData.Limit, locale, mockClient).Result;
        }

        public IGeoSpatialService InitRoutingMapsService(BotSettings services, string locale = "en-us")
        {
            var key = GetAzureMapsKey(services);

            return new AzureMapsGeoSpatialService().InitKeyAsync(key, MockData.Radius, MockData.Limit, locale, mockClient).Result;
        }

        protected string GetAzureMapsKey(BotSettings services)
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
