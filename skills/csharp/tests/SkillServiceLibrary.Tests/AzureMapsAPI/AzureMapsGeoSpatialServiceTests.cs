// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkillServiceLibrary.Fakes.AzureMapsAPI.Fakes;
using SkillServiceLibrary.Services.AzureMapsAPI;

namespace SkillServiceLibrary.Tests.AzureMapsAPI
{
    [TestClass]
    [TestCategory("UnitTests")]
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

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.RouteLimit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(MockData.Latitude, MockData.Longitude);
            Assert.AreEqual(pointOfInterestList[0].Id, "US/POI/p1/101761");
            Assert.AreEqual(pointOfInterestList[0].Name, "Microsoft Way");
            Assert.AreEqual(pointOfInterestList[0].Address, "157th Ave NE, Redmond, WA 98052");
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Latitude, 47.63954);
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Longitude, -122.1307);
            Assert.AreEqual(pointOfInterestList[0].Category, "Bus Stop");
        }

        [TestMethod]
        public async Task GetPointOfInterestDetailsTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.RouteLimit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(MockData.Latitude, MockData.Longitude);

            var pointOfInterest = await service.GetPointOfInterestDetailsAsync(pointOfInterestList[0]);
            Assert.AreEqual(pointOfInterest.PointOfInterestImageUrl.Substring(0, 23), "data:image/jpeg;base64,");
        }

        [TestMethod]
        public async Task GetPointsOfInterestByQueryTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.RouteLimit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(MockData.Latitude, MockData.Longitude, MockData.Query);
            Assert.AreEqual(pointOfInterestList[0].Id, "US/POI/p1/101761");
            Assert.AreEqual(pointOfInterestList[0].Name, "Microsoft Way");
            Assert.AreEqual(pointOfInterestList[0].Address, "157th Ave NE, Redmond, WA 98052");
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Latitude, 47.63954);
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Longitude, -122.1307);
            Assert.AreEqual(pointOfInterestList[0].Category, "Bus Stop");
        }

        [TestMethod]
        public async Task GetPointsOfInterestByCategoryTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.RouteLimit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetPointOfInterestListByCategoryAsync(MockData.Latitude, MockData.Longitude, MockData.Query);
            Assert.AreEqual(pointOfInterestList.Count, 3);

            pointOfInterestList = await service.GetPointOfInterestListByCategoryAsync(MockData.Latitude, MockData.Longitude, MockData.Query, null, true);
            Assert.AreEqual(pointOfInterestList.Count, 2);
        }

        [TestMethod]
        public async Task GetRouteDirectionsTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.RouteLimit, MockData.Locale, mockClient);

            var routeDirections = await service.GetRouteDirectionsToDestinationAsync(MockData.Latitude, MockData.Longitude, MockData.Latitude, MockData.Longitude);
            Assert.AreEqual(routeDirections.Routes[0].Summary.LengthInMeters, 1147);
            Assert.AreEqual(routeDirections.Routes[0].Summary.TravelTimeInSeconds, 162);
        }

        [TestMethod]
        public async Task GetAddressSearchTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.RouteLimit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetPointOfInterestListByAddressAsync(MockData.Latitude, MockData.Longitude, MockData.Address);
            Assert.AreEqual(pointOfInterestList[0].Address, "1635 11th Avenue Northwest, Issaquah, WA 98027");
            Assert.AreEqual(pointOfInterestList[0].AddressAlternative, "11th Avenue Northwest, Issaquah, King, Washington, USA");
            Assert.AreEqual(pointOfInterestList[0].Category, "Address Range");
        }

        [TestMethod]
        public async Task GetParkingCategoryTest()
        {
            var service = new AzureMapsGeoSpatialService();

            await service.InitKeyAsync(MockData.Key, MockData.Radius, MockData.Limit, MockData.RouteLimit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetPointOfInterestListByParkingCategoryAsync(MockData.Latitude, MockData.Longitude);
            Assert.AreEqual(pointOfInterestList[0].Name, "1110 Elliott Avenue West");
            Assert.AreEqual(pointOfInterestList[1].Name, "1108 Elliott Ave W");
            Assert.AreEqual(pointOfInterestList[2].Name, "660 Elliott Avenue West");
        }
    }
}
