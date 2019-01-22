using System.Collections.Generic;
using System.Threading.Tasks;
using EmailSkill.ServiceClients;
using EmailSkill.ServiceClients.GoogleAPI;
using EmailSkillTest.API.Fakes.Google;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.API.Service
{
    [TestClass]
    public class GoogleUserServiceTests
    {
        public static IUserService UserService { get; set; }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            UserService = new GooglePeopleService(MockGoogleUserClient.MockGoogleUserService.Object);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestInitialize]
        public void TestInit()
        {
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public async Task GetPeopleAsyncTest()
        {
            List<Person> result = await UserService.GetPeopleAsync("Doe");
            Assert.IsTrue(result.Count == 2);
        }

        [TestMethod]
        public async Task GetUserAsyncTest()
        {
            List<User> result = await UserService.GetUserAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod]
        public async Task GetContactsAsyncTest()
        {
            List<Contact> result = await UserService.GetContactsAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }
    }
}