using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.ServiceClients;
using PointOfInterestSkillTests.API.Fakes;

namespace PointOfInterestSkillTests.API
{
    [TestClass]
    public class AzureMapsGeoSpatialServiceTests
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
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(MockData.Latitude, MockData.Longitude);
            Assert.AreEqual(pointOfInterestList[0].Id, "US/POI/p1/101761");
            Assert.AreEqual(pointOfInterestList[0].Name, "Microsoft Way");
            Assert.AreEqual(pointOfInterestList[0].Street, "157th Ave NE");
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Latitude, 47.63954);
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Longitude, -122.1307);
            Assert.AreEqual(pointOfInterestList[0].Category, "Bus Stop");
        }

        [TestMethod]
        public async Task GetPointOfInterestDetailsTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(MockData.Latitude, MockData.Longitude);


            var pointOfInterest = await service.GetPointOfInterestDetailsAsync(pointOfInterestList[0]);
            Assert.AreEqual(pointOfInterest.ImageUrl, string.Format("https://atlas.microsoft.com/map/static/png?api-version=1.0&layer=basic&style=main&zoom={2}&center={1},{0}&width=512&height=512&subscription-key={3}", pointOfInterestList[0].Geolocation.Latitude, pointOfInterestList[0].Geolocation.Longitude, 15, MockData.Key));
        }

        [TestMethod]
        public async Task GetPointsOfInterestByQueryTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(MockData.Latitude, MockData.Longitude, MockData.Query);
            Assert.AreEqual(pointOfInterestList[0].Id, "US/POI/p1/101761");
            Assert.AreEqual(pointOfInterestList[0].Name, "Microsoft Way");
            Assert.AreEqual(pointOfInterestList[0].Street, "157th Ave NE");
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Latitude, 47.63954);
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Longitude, -122.1307);
            Assert.AreEqual(pointOfInterestList[0].Category, "Bus Stop");
        }

        [TestMethod]
        public async Task GetRouteDirectionsTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.Locale, mockClient);

            var routeDirections = await service.GetRouteDirectionsToDestinationAsync(MockData.Latitude, MockData.Longitude, MockData.Latitude, MockData.Longitude);
            Assert.AreEqual(routeDirections.Routes[0].Summary.LengthInMeters, 1147);
            Assert.AreEqual(routeDirections.Routes[0].Summary.TravelTimeInSeconds, 162);
        }

        [TestMethod]
        public async Task GetAddressSearchTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetPointOfInterestListByAddressAsync(MockData.Latitude, MockData.Longitude, MockData.Address);
            Assert.AreEqual(pointOfInterestList[0].City, "Issaquah");
            Assert.AreEqual(pointOfInterestList[1].City, "West Queen Anne, Seattle");
            Assert.AreEqual(pointOfInterestList[2].City, "Norkirk, Kirkland");
        }

        [TestMethod]
        public async Task GetParkingCategoryTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetPointOfInterestListByParkingCategoryAsync(MockData.Latitude, MockData.Longitude);
            Assert.AreEqual(pointOfInterestList[0].Name, "1110 Elliott Avenue West");
            Assert.AreEqual(pointOfInterestList[1].Name, "1108 Elliott Ave W");
            Assert.AreEqual(pointOfInterestList[2].Name, "660 Elliott Avenue West");
        }


    }
}
