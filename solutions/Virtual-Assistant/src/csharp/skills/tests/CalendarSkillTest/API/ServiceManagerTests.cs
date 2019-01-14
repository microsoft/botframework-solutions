using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkillTest.API.Fakes;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalendarSkillTest.API
{
    [TestClass]
    public class ServiceManagerTests
    {
        private static IServiceManager serviceManager;
        private static Mock<SkillConfigurationBase> mockConfig;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            mockConfig = new Mock<SkillConfigurationBase>();
            mockConfig.SetupGet(config => config.Properties).Returns(() =>
            {
                Dictionary<string, object> props = new Dictionary<string, object>
                {
                    { "googleAppName", "testAppName" },
                    { "googleClientId", "testClientId" },
                    { "googleClientSecret", "testClientSecret" },
                    { "googleScopes", "testScopes" }
                };

                return props;
            });
            serviceManager = new ServiceManager(mockConfig.Object);
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
        public void GetMSUserServiceTest()
        {
            IUserService userService = serviceManager.InitUserService("test token", EventSource.Microsoft);
            Assert.IsTrue(userService is UserService);
        }

        [TestMethod]
        public void GetMSCalendarServiceTest()
        {
            ICalendarService calendarService = serviceManager.InitCalendarService("test token", EventSource.Microsoft);
            Assert.IsTrue(calendarService is CalendarService);
        }

        [TestMethod]
        public void GetGoogleUserServiceTest()
        {
            IUserService userService = serviceManager.InitUserService("test token", EventSource.Google);
            Assert.IsTrue(userService is UserService);
        }

        [TestMethod]
        public void GetGoogleCalendarServiceTest()
        {
            ICalendarService calendarService = serviceManager.InitCalendarService("test token", EventSource.Google);
            Assert.IsTrue(calendarService is CalendarService);
        }

        [TestMethod]
        public void GetOtherUserServiceTest_Throws()
        {
            try
            {
                IUserService userService = serviceManager.InitUserService("test token", EventSource.Other);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == "Event Type not Defined");
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public void GetOtherCalendarServiceTest_Throws()
        {
            try
            {
                ICalendarService calendarService = serviceManager.InitCalendarService("test token", EventSource.Other);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == "Event Type not Defined");
                return;
            }

            Assert.Fail("Should throw exception");
        }
    }
}
