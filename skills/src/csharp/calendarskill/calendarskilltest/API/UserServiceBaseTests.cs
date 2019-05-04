using System.Threading.Tasks;
using CalendarSkill.Services;
using CalendarSkillTest.API.Fakes.MockBaseClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.API
{
    [TestClass]
    public class UserServiceBaseTests
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
            userService = new UserService(MockBaseUserClient.GetUserService());
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public async Task GetPeopleAsyncTest()
        {
            var result = await userService.GetPeopleAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod]
        public async Task GetUserAsyncTest()
        {
            var result = await userService.GetUserAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod]
        public async Task GetContactsAsyncTest()
        {
            var result = await userService.GetContactsAsync("Doe");
            Assert.IsTrue(result.Count == 0);
        }
    }
}
