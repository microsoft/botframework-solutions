using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.Services;
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

            await service.InitClientAsync(MockData.ClientId, MockData.ClientSecret, MockData.Radius, MockData.Limit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(MockData.Latitude, MockData.Longitude);

            Assert.AreEqual(pointOfInterestList[0].Id, "412d2800f964a520df0c1fe3");
            Assert.AreEqual(pointOfInterestList[0].Name, "Central Park");
            Assert.AreEqual(pointOfInterestList[0].Address, "59th St to 110th St (5th Ave to Central Park West), New York, NY 10028");
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Latitude, 40.784084320068359);
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Longitude, -73.964851379394531);
            Assert.AreEqual(pointOfInterestList[0].Category, "Park");
            Assert.AreEqual(pointOfInterestList[0].Price, 0);
            Assert.AreEqual(pointOfInterestList[0].Hours, "Open until 1:00 AM");
            Assert.AreEqual(pointOfInterestList[0].Rating, "9.8");
            Assert.AreEqual(pointOfInterestList[0].RatingCount, 18854);
        }

        [TestMethod]
        public async Task GetPointOfInterestDetailsTest()
        {
            var service = new FoursquareGeoSpatialService();

            await service.InitClientAsync(MockData.ClientId, MockData.ClientSecret, MockData.Radius, MockData.Limit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(MockData.Latitude, MockData.Longitude);

            var pointOfInterest = await service.GetPointOfInterestDetailsAsync(pointOfInterestList[0]);
            Assert.AreEqual(pointOfInterest.Id, "412d2800f964a520df0c1fe3");

            Assert.AreEqual(pointOfInterest.Name, "Central Park");
            Assert.AreEqual(pointOfInterestList[0].Address, "59th St to 110th St (5th Ave to Central Park West), New York, NY 10028");
            Assert.AreEqual(pointOfInterest.Geolocation.Latitude, 40.784084320068359);
            Assert.AreEqual(pointOfInterest.Geolocation.Longitude, -73.964851379394531);
            Assert.AreEqual(pointOfInterest.Category, "Park");
            Assert.AreEqual(pointOfInterest.Price, 0);
            Assert.AreEqual(pointOfInterest.Hours, "Open until 1:00 AM");
            Assert.AreEqual(pointOfInterest.Rating, "9.8");
            Assert.AreEqual(pointOfInterest.RatingCount, 18854);
        }

        [TestMethod]
        public async Task GetPointsOfInterestByQueryTest()
        {
            var service = new FoursquareGeoSpatialService();

            await service.InitClientAsync(MockData.ClientId, MockData.ClientSecret, MockData.Radius, MockData.Limit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(MockData.Latitude, MockData.Longitude, MockData.Query);
            Assert.AreEqual(pointOfInterestList[0].Id, "412d2800f964a520df0c1fe3");
            Assert.AreEqual(pointOfInterestList[0].Name, "Central Park");
            Assert.AreEqual(pointOfInterestList[0].Address, "59th St to 110th St (5th Ave to Central Park West), New York, NY 10028");
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Latitude, 40.784084320068359);
            Assert.AreEqual(pointOfInterestList[0].Geolocation.Longitude, -73.964851379394531);
            Assert.AreEqual(pointOfInterestList[0].Category, "Park");
        }

        [TestMethod]
        public async Task GetParkingCategoryTest()
        {
            var service = new FoursquareGeoSpatialService();

            await service.InitClientAsync(MockData.ClientId, MockData.ClientSecret, MockData.Radius, MockData.Limit, MockData.Locale, mockClient);

            var pointOfInterestList = await service.GetPointOfInterestListByParkingCategoryAsync(MockData.Latitude, MockData.Longitude);
            Assert.AreEqual(pointOfInterestList[0].Name, "Sea-Tac Airport Parking Garage");
            Assert.AreEqual(pointOfInterestList[1].Name, "Bellevue Square Parking Garage");
            Assert.AreEqual(pointOfInterestList[2].Name, "Sea-Tac Cell Phone Lot");
        }
    }
}
