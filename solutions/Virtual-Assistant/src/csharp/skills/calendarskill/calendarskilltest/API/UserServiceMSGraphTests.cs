using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkill.ServiceClients.MSGraphAPI;
using CalendarSkillTest.API.Fakes.MockMSGraphClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.API
{
    [TestClass]
    public class UserServiceMSGraphTests
    {
        private static IUserService userService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestInitialize]
        public void TestInit()
        {
            userService = new UserService(new MSGraphUserService(MockMSGraphUserClient.GetUserService()));
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
            Assert.IsTrue(result.Count == 2);
        }

        [TestMethod]
        public async Task GetContactsAsyncTest()
        {
            List<PersonModel> result = await userService.GetContactsAsync("Doe");
            Assert.IsTrue(result.Count == 2);
        }
    }
}
