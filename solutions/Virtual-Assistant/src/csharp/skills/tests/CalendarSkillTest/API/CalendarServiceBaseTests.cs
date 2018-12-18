using System;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkillTest.API.Fakes.MockBaseClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.API
{
    // this will test all logic in CalendarService only.
    // only have success test now
    // todo: add more error test
    [TestClass]
    public class CalendarServiceBaseTests
    {
        private static ICalendarService calendarService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            calendarService = new CalendarService(MockBaseServiceClient.GetCalendarService(), EventSource.Microsoft);
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
        public async Task CreateEventTest()
        {
            EventModel newEvent = new EventModel(EventSource.Microsoft);
            await calendarService.CreateEvent(newEvent);
        }

        [TestMethod]
        public async Task GetUpComingEventsTest()
        {
            await calendarService.GetUpcomingEvents();
        }

        [TestMethod]
        public async Task GetEventsByTimeTest()
        {
            await calendarService.GetEventsByTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc), DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc));
        }

        [TestMethod]
        public async Task GetEventsByTimeTest_StartTimeNotUtc_Throws()
        {
            try
            {
                await calendarService.GetEventsByTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Local), DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc));
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("Time is not UTC"));
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public async Task GetEventsByTimeTest_EndTimeNotUtc_Throws()
        {
            try
            {
                await calendarService.GetEventsByTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc), DateTime.SpecifyKind(new DateTime(), DateTimeKind.Local));
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("Time is not UTC"));
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public async Task GetEventsByStartTimeTest()
        {
            await calendarService.GetEventsByStartTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc));
        }

        [TestMethod]
        public async Task GetEventsByStartTimeTest_NotUtc_Throws()
        {
            try
            {
                await calendarService.GetEventsByStartTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Local));
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("Time is not UTC"));
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public async Task GetEventsByTitle()
        {
            await calendarService.GetEventsByTitle("test");
        }

        [TestMethod]
        public async Task DeleteEventsById()
        {
            await calendarService.DeleteEventById("test id");
        }

        [TestMethod]
        public async Task UpdateEventsById()
        {
            EventModel updateEvent = new EventModel(EventSource.Microsoft)
            {
                Id = "update_event",
                TimeZone = TimeZoneInfo.Utc,
                StartTime = DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc),
                EndTime = DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc)
            };

            EventModel updateResult = await calendarService.UpdateEventById(updateEvent);
            Assert.IsTrue(updateEvent.Id == updateResult.Id);
        }
    }
}
