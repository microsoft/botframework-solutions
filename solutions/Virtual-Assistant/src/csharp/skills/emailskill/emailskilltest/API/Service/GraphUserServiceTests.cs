using System;
using System.Threading.Tasks;
using EmailSkill.ServiceClients.MSGraphAPI;
using EmailSkillTest.API.Fakes.MSGraph;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EmailSkillTest.API.Service
{
    [TestClass]
    public class GraphUserServiceTests
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

            var mockGraphServiceClient = new MockGraphServiceClient
            {
                Users = users
            };
            mockGraphServiceClient.SetMockBehavior();

            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphUserService userService = new MSGraphUserService(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

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

            var mockGraphServiceClient = new MockGraphServiceClient
            {
                People = people
            };
            mockGraphServiceClient.SetMockBehavior();

            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphUserService userService = new MSGraphUserService(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            var result = await userService.GetPeopleAsync("test");

            Assert.IsTrue(result.Count == 12);

            // "Conf Room" is filtered
            foreach (var user in result)
            {
                Assert.IsFalse(user.DisplayName.StartsWith("Conf Room"));
            }
        }

        [TestMethod]
        public async Task GetContactsTest()
        {
            IUserContactsCollectionPage contacts = new UserContactsCollectionPage();
            for (int i = 0; i < 3; i++)
            {
                var contact = new Contact()
                {
                    DisplayName = "Conf Room" + i,
                };

                contacts.Add(contact);
            }

            for (int i = 0; i < 12; i++)
            {
                var contact = new Contact()
                {
                    DisplayName = "TestUser" + i,
                };

                contacts.Add(contact);
            }

            var mockGraphServiceClient = new MockGraphServiceClient
            {
                Contacts = contacts
            };
            mockGraphServiceClient.SetMockBehavior();

            IGraphServiceClient serviceClient = mockGraphServiceClient.GetMockGraphServiceClient().Object;
            MSGraphUserService userService = new MSGraphUserService(serviceClient, timeZoneInfo: TimeZoneInfo.Local);

            var result = await userService.GetContactsAsync("test");

            Assert.IsTrue(result.Count == 12);

            // "Conf Room" is filtered
            foreach (var user in result)
            {
                Assert.IsFalse(user.DisplayName.StartsWith("Conf Room"));
            }
        }
    }
}