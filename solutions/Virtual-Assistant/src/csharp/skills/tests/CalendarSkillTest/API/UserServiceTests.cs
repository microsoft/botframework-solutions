using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using CalendarSkillTest.API.Fakes;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.API
{
    [TestClass]
    public class UserServiceTests
    {

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
        }

        [TestMethod]
        public async Task GetUserTest()
        {
            IGraphServiceUsersCollectionPage users = new GraphServiceUsersCollectionPage();
            for (int i = 0; i < 3; i++)
            {
                var user = new User()
                {
                    DisplayName = "Conf Room" + i,
                };

                users.Add(user);
            }

            for (int i = 0; i < 12; i++)
            {
                var user = new User()
                {
                    DisplayName = "TestUser" + i,
                };

                users.Add(user);
            }

            var mockGraphServiceClient = new MockGraphServiceClientGen();
            mockGraphServiceClient.Users = users;
            mockGraphServiceClient.SetMockBehavior();

            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            IServiceManager serviceManager = new ServiceManager();
            IUserService userService = serviceManager.InitUserService(serviceClient, TimeZoneInfo.Local);

            var result = await userService.GetUserAsync("test");


            // Test get 0-10 user per page
            Assert.IsTrue(result.Count >= 1);
            Assert.IsTrue(result.Count <= 10);

            // "Conf Room" is filtered
            foreach (var user in result)
            {
                Assert.IsFalse(user.DisplayName.StartsWith("Conf Room"));
            }
        }

        [TestMethod]
        public async Task GetPeopleTest()
        {
            IUserPeopleCollectionPage people = new UserPeopleCollectionPage();
            for (int i = 0; i < 3; i++)
            {
                var person = new Person()
                {
                    DisplayName = "Conf Room" + i,
                };

                people.Add(person);
            }

            for (int i = 0; i < 12; i++)
            {
                var user = new Person()
                {
                    DisplayName = "TestUser" + i,
                };

                people.Add(user);
            }

            var mockGraphServiceClient = new MockGraphServiceClientGen();
            mockGraphServiceClient.People = people;
            mockGraphServiceClient.SetMockBehavior();

            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            IServiceManager serviceManager = new ServiceManager();
            IUserService userService = serviceManager.InitUserService(serviceClient, TimeZoneInfo.Local);

            var result = await userService.GetPeopleAsync("test");

            // Test get > 0 people per page
            Assert.IsTrue(result.Count == 12);

            // "Conf Room" is filtered
            foreach (var user in result)
            {
                Assert.IsFalse(user.DisplayName.StartsWith("Conf Room"));
            }
        }
    }
}
