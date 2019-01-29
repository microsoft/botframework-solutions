using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.ServiceClients;
using PointOfInterestSkillTests.API.Fakes;

namespace PointOfInterestSkillTests.API
{
    [TestClass]
    public class FoursquareGeoSpatialServiceTests
    {
        private HttpClient mockClient;

        [TestInitialize]
        public void Initialize()
        {
            mockClient = new HttpClient(new MockHttpClientHandlerGen().GetMockHttpClientHandler());
        }


        [TestMethod]
        public async Task GetNearbyPointsOfInterestTest()
        {
            var service = new FoursquareGeoSpatialService();

            await service.InitClientAsync(MockData.ClientId, MockData.ClientSecret, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetNearbyPointsOfInterestAsync(MockData.Latitude, MockData.Longitude);
        }

        [TestMethod]
        public async Task GetPointOfInterestDetailsTest()
        {
            //var service = new FoursquareGeoSpatialService();

            //await service.InitClientAsync(MockData.ClientId, MockData.ClientSecret, MockData.Locale, mockClient);

            //var pointOfInterestList = await service.GetPointOfInterestDetailsAsync(MockData.PointOfInterest);

            throw new NotImplementedException();
        }

        [TestMethod]
        public async Task GetPointsOfInterestByQueryTest()
        {
            var service = new FoursquareGeoSpatialService();

            await service.InitClientAsync(MockData.ClientId, MockData.ClientSecret, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetPointOfInterestByQueryAsync(MockData.Latitude, MockData.Longitude, MockData.Query, MockData.Country);
        }
    }
}
