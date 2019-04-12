using System;
using CalendarSkill.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.Common
{
    [TestClass]
    public class TimeConverterTests
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
        public void ConvertLuisLocalToUtcTest()
        {
            var testTime = new DateTime(2020, 1, 1, 8, 0, 0, DateTimeKind.Local);
            var timezone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            DateTime resultTime = TimeConverter.ConvertLuisLocalToUtc(testTime, timezone);

            var expectTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(resultTime, expectTime);
        }

        [TestMethod]
        public void ConvertUtcToUserTimeTest()
        {
            var testTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timezone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
            DateTime resultTime = TimeConverter.ConvertUtcToUserTime(testTime, timezone);

            var expectTime = new DateTime(2020, 1, 1, 8, 0, 0, DateTimeKind.Local);
            Assert.AreEqual(resultTime, expectTime);
        }

        [TestMethod]
        public void ConvertUtcToUserTimeTest_NotUtc_Throws()
        {
            try
            {
                var testTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Local);
                var timezone = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
                DateTime resultTime = TimeConverter.ConvertUtcToUserTime(testTime, timezone);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == "Input time is not a UTC time.");
                return;
            }

            Assert.Fail("Should throw exception");
        }
    }
}