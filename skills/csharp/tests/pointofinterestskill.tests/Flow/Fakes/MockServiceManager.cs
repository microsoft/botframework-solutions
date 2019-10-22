// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using PointOfInterestSkill.Services;
using SkillServiceLibrary.Fakes.AzureMapsAPI.Fakes;
using SkillServiceLibrary.Services;
using SkillServiceLibrary.Services.AzureMapsAPI;
using SkillServiceLibrary.Services.FoursquareAPI;

namespace PointOfInterestSkill.Tests.API.Fakes
{
    public class MockServiceManager : IServiceManager
    {
        private HttpClient mockClient;

        public MockServiceManager()
        {
            mockClient = new HttpClient(new MockHttpClientHandlerGen().GetMockHttpClientHandler());
        }

        public IGeoSpatialService InitMapsService(BotSettings settings, string locale = "en-us")
        {
            var clientIdStr = settings.FoursquareClientId;
            var clientSecretStr = settings.FoursquareClientSecret;

            if (clientIdStr != null && clientSecretStr != null)
            {
                return new FoursquareGeoSpatialService().InitClientAsync(clientIdStr, clientSecretStr, MockData.Radius, MockData.Limit, MockData.RouteLimit, MockData.Locale, mockClient).Result;
            }
            else
            {
                var key = GetAzureMapsKey(settings);

                return new AzureMapsGeoSpatialService().InitKeyAsync(key, MockData.Radius, MockData.Limit, MockData.RouteLimit, locale, mockClient).Result;
            }
        }

        public IGeoSpatialService InitAddressMapsService(BotSettings services, string locale = "en-us")
        {
            var key = GetAzureMapsKey(services);

            return new AzureMapsGeoSpatialService().InitKeyAsync(key, MockData.Radius, MockData.Limit, MockData.RouteLimit, locale, mockClient).Result;
        }

        public IGeoSpatialService InitRoutingMapsService(BotSettings services, string locale = "en-us")
        {
            var key = GetAzureMapsKey(services);

            return new AzureMapsGeoSpatialService().InitKeyAsync(key, MockData.Radius, MockData.Limit, MockData.RouteLimit, locale, mockClient).Result;
        }

        protected string GetAzureMapsKey(BotSettings settings)
        {
            var keyStr = settings.AzureMapsKey;
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
