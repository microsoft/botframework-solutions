using System;
using CalendarSkill.Models;
using CalendarSkill.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.API
{
    [TestClass]
    public class ServiceManagerTests
    {
        private static IServiceManager serviceManager;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var mockConfig = new BotSettings
            {
                GoogleAppName = "testAppName",
                GoogleClientId = "testClientId",
                GoogleClientSecret = "testClientSecret",
                GoogleScopes = "testScopes"
            };

            serviceManager = new ServiceManager(mockConfig);
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
            var userService = serviceManager.InitUserService("test token", EventSource.Microsoft);
            Assert.IsTrue(userService is UserService);
        }

        [TestMethod]
        public void GetMSCalendarServiceTest()
        {
            var calendarService = serviceManager.InitCalendarService("test token", EventSource.Microsoft);
            Assert.IsTrue(calendarService is CalendarService);
        }

        [TestMethod]
        public void GetGoogleUserServiceTest()
        {
            var userService = serviceManager.InitUserService("test token", EventSource.Google);
            Assert.IsTrue(userService is UserService);
        }

        [TestMethod]
        public void GetGoogleCalendarServiceTest()
        {
            var calendarService = serviceManager.InitCalendarService("test token", EventSource.Google);
            Assert.IsTrue(calendarService is CalendarService);
        }

        [TestMethod]
        public void GetOtherUserServiceTest_Throws()
        {
            try
            {
                var userService = serviceManager.InitUserService("test token", EventSource.Other);
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
                var calendarService = serviceManager.InitCalendarService("test token", EventSource.Other);
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
