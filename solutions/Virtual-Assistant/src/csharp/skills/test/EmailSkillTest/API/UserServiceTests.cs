using System;
using System.Collections.Generic;
using System.Text;

namespace EmailSkillTest.API
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using EmailSkill;
    //using EmailSkillTest.Fakes;
    using Microsoft.Graph;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UserServiceTests
    {
        private static UserService userService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            userService = null;//new UserService(string.Empty, timeZoneInfo: TimeZoneInfo.Local);
        }

        [TestMethod]
        public async Task GetUserTest()
        {
            var users = await userService.GetUserAsync("test");
            Assert.IsTrue(users.Count >= 1);
        }

        [TestMethod]
        public async Task GetPeopleTest()
        {
            var people = await userService.GetPeopleAsync("test");
            Assert.IsTrue(people.Count >= 1);
        }
    }
}
