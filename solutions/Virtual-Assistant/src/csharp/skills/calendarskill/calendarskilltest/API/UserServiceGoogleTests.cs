using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkill.ServiceClients.GoogleAPI;
using CalendarSkillTest.API.Fakes.MockGoogleClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.API
{
    [TestClass]
    public class UserServiceGoogleTests
    {
        private static IUserService userService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            userService = new UserService(new GooglePeopleService(MockGoogleUserClient.GetPeopleService()));
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
            List<PersonModel> result = await userService.GetPeopleAsync("Doe");
            Assert.IsTrue(result.Count == 2);
        }

        [TestMethod]
        public async Task GetUserAsyncTest()
        {
            List<PersonModel> result = await userService.GetUserAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod]
        public async Task GetContactsAsyncTest()
        {
            List<PersonModel> result = await userService.GetContactsAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }
    }
}
