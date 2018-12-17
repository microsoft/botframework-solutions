using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using CalendarSkill.Common;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkill.ServiceClients.GoogleAPI;
using CalendarSkill.ServiceClients.MSGraphAPI;
using CalendarSkillTest.API.Fakes;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.API
{
    [TestClass]
    public class TimeZoneConverterTests
    {
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
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public async Task IanaToWindowsTest()
        {
            string input = "Asia/Shanghai";
            string result = TimeZoneConverter.IanaToWindows(input);
            string expect = "China Standard Time";
            Assert.AreEqual(result, expect);
        }

        [TestMethod]
        public async Task WindowsToIanaTest()
        {
            string input = "China Standard Time";
            string result = TimeZoneConverter.WindowsToIana(input);
            string expect = "Asia/Shanghai";
            Assert.AreEqual(result, expect);
        }

        [TestMethod]
        public async Task IanaToWindowsTest_NotLegal_Throws()
        {
            try
            {
                string input = "test";
                string result = TimeZoneConverter.IanaToWindows(input);
            }
            catch (InvalidTimeZoneException e)
            {
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public async Task WindowsToIanaTest_NotLegal_Throws()
        {
            try
            {
                string input = "test";
                string result = TimeZoneConverter.WindowsToIana(input);
            }
            catch (InvalidTimeZoneException e)
            {
                return;
            }

            Assert.Fail("Should throw exception");
        }
    }
}
