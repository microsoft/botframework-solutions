using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Services;
using CalendarSkill.Services.MSGraphAPI;
using CalendarSkillTest.API.Fakes.MockMSGraphClient;
using Microsoft.Bot.Builder.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.API
{
    // this will test all logic in MSGraph Calendar service
    // only have success test now
    // todo: add more error test
    [TestClass]
    public class CalendarServiceMSGrapghTests
    {
        private static ICalendarService calendarService;

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
            calendarService = new CalendarService(new MSGraphCalendarAPI(MockMSGraphServiceClient.GetCalendarService()), EventSource.Microsoft);
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public async Task CreateEventTest()
        {
            var createEvent = new EventModel(new Microsoft.Graph.Event())
            {
                Id = "create_event"
            };
            EventModel createResult = await calendarService.CreateEvent(createEvent);
            Assert.IsTrue(createEvent.Id == createResult.Id);
        }

        [TestMethod]
        public async Task GetUpcomingEventsTest()
        {
            List<EventModel> events = await calendarService.GetUpcomingEvents();
            Assert.IsTrue(events.Count == 1);
        }

        [TestMethod]
        public async Task GetEventsByTimeTest()
        {
            var startTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T18:00:00.0000000Z"));
            var endTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T19:00:00.0000000Z"));
            List<EventModel> events = await calendarService.GetEventsByTime(startTime, endTime);
            Assert.IsTrue(events.Count == 1);
        }

        [TestMethod]
        public async Task GetEventsByStartTimeTest()
        {
            var startTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T18:00:00.0000000Z"));
            List<EventModel> events = await calendarService.GetEventsByStartTime(startTime);
            Assert.IsTrue(events.Count == 1);
        }

        [TestMethod]
        public async Task GetEventsByTitleTest()
        {
            List<EventModel> events = await calendarService.GetEventsByTitle("test");
            Assert.IsTrue(events.Count == 1);
        }

        [TestMethod]
        public async Task UpdateEventByIdTest()
        {
            var updateEvent = new EventModel(new Microsoft.Graph.Event())
            {
                Id = "update_event"
            };
            EventModel updateResult = await calendarService.UpdateEventById(updateEvent);
            Assert.IsTrue(updateEvent.Id == updateResult.Id);
        }

        [TestMethod]
        public async Task DeleteEventByIdTest()
        {
            var deleteId = "delete_event";
            await calendarService.DeleteEventById(deleteId);
        }

        [TestMethod]
        public async Task DeleteEventByIdTest_EventNotExist_Throws()
        {
            try
            {
                var deleteId = "delete_not_exist_event";
                await calendarService.DeleteEventById(deleteId);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == "Event id not found");
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public async Task DeleteEventByIdTest_AccessDenied_Throws()
        {
            try
            {
                var deleteId = "Test_Access_Denied";
                await calendarService.DeleteEventById(deleteId);
            }
            catch (SkillException e)
            {
                Assert.IsTrue(e.ExceptionType == SkillExceptionType.APIAccessDenied);
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public async Task DeclineEventByIdTest()
        {
            var declineId = "decline_event";
            await calendarService.DeclineEventById(declineId);
        }

        [TestMethod]
        public async Task DeclineEventByIdTest_EventNotExist_Throws()
        {
            try
            {
                var declineId = "decline_not_exist_event";
                await calendarService.DeleteEventById(declineId);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == "Event id not found");
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public async Task AcceptEventByIdTest()
        {
            var acceptId = "accept_event";
            await calendarService.AcceptEventById(acceptId);
        }

        [TestMethod]
        public async Task AcceptEventByIdTest_EventNotExist_Throws()
        {
            try
            {
                var acceptId = "accept_not_exist_event";
                await calendarService.AcceptEventById(acceptId);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == "Event id not found");
                return;
            }

            Assert.Fail("Should throw exception");
        }
    }
}
