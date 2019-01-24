using System;
using CalendarSkill.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Common
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
        public void IanaToWindowsTest()
        {
            string input = "Asia/Shanghai";
            string result = TimeZoneConverter.IanaToWindows(input);
            string expect = "China Standard Time";
            Assert.AreEqual(result, expect);
        }

        [TestMethod]
        public void WindowsToIanaTest()
        {
            string input = "China Standard Time";
            string result = TimeZoneConverter.WindowsToIana(input);
            string expect = "Asia/Shanghai";
            Assert.AreEqual(result, expect);
        }

        [TestMethod]
        public void IanaToWindowsTest_NotLegal_Throws()
        {
            try
            {
                string input = "test";
                string result = TimeZoneConverter.IanaToWindows(input);
            }
            catch (InvalidTimeZoneException)
            {
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public void WindowsToIanaTest_NotLegal_Throws()
        {
            try
            {
                string input = "test";
                string result = TimeZoneConverter.WindowsToIana(input);
            }
            catch (InvalidTimeZoneException)
            {
                return;
            }

            Assert.Fail("Should throw exception");
        }
    }
}