using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkill.ServiceClients.GoogleAPI;
using CalendarSkillTest.API.Fakes.MockGoogleClient;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.API
{
    // this will test all logic in Google Calendar service
    // only have success test now
    // todo: add more error test
    [TestClass]
    public class CalendarServiceGoogleTests
    {
        private static ICalendarService calendarService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            calendarService = new CalendarService(new GoogleCalendarAPI(MockGoogleServiceClient.GetCalendarService()), EventSource.Google);
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
            EventModel createEvent = new EventModel(new Google.Apis.Calendar.v3.Data.Event())
            {
                Id = "create_event"
            };
            EventModel createResult = await calendarService.CreateEvent(createEvent);
            Assert.IsTrue(createEvent.Id == createResult.Id);
        }

        [TestMethod]
        public async Task GetUpcomingEventsTest()
        {
            List<EventModel> upcomingEvents = await calendarService.GetUpcomingEvents();
            Assert.IsTrue(upcomingEvents.Count == 4);
        }

        [TestMethod]
        public async Task GetEventsByTimeTest()
        {
            DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T18:00:00.0000000Z"));
            DateTime endTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T19:00:00.0000000Z"));
            List<EventModel> events = await calendarService.GetEventsByTime(startTime, endTime);
            Assert.IsTrue(events.Count == 3);
        }

        [TestMethod]
        public async Task GetEventsByStartTimeTest()
        {
            DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T18:00:00.0000000Z"));
            List<EventModel> events = await calendarService.GetEventsByStartTime(startTime);
            Assert.IsTrue(events.Count == 2);
        }

        [TestMethod]
        public async Task GetEventsByTitleTest()
        {
            List<EventModel> events = await calendarService.GetEventsByTitle("same_name_event");
            Assert.IsTrue(events.Count == 2);
        }

        [TestMethod]
        public async Task UpdateEventByIdTest()
        {
            EventModel updateEvent = new EventModel(EventSource.Google)
            {
                Id = "update_event"
            };
            EventModel updateResult = await calendarService.UpdateEventById(updateEvent);
            Assert.IsTrue(updateEvent.Id == updateResult.Id);
        }

        [TestMethod]
        public async Task DeleteEventByIdTest()
        {
            string deleteId = "delete_event";
            await calendarService.DeleteEventById(deleteId);
        }

        [TestMethod]
        public async Task DeleteEventByIdTest_EventNotExist_Throws()
        {
            try
            {
                string deleteId = "delete_not_exist_event";
                await calendarService.DeleteEventById(deleteId);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == MockGoogleServiceClient.EventIdNotFound);
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public async Task DeleteEventByIdTest_AccessDenied_Throws()
        {
            try
            {
                string deleteId = "Test_Access_Denied";
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
        public async Task AcceptEventByIdTest()
        {
            string eventId = "Get_Not_Org_Event";
            await calendarService.AcceptEventById(eventId);
        }

        [TestMethod]
        public async Task AcceptEventByIdTest_EventNotExist_Throws()
        {
            try
            {
                string eventId = "Get_Event_Not_Exist";
                await calendarService.AcceptEventById(eventId);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == MockGoogleServiceClient.EventIdNotFound);
                return;
            }

            Assert.Fail("Should throw exception");
        }

        [TestMethod]
        public async Task DeclineEventByIdTest()
        {
            string eventId = "Get_Not_Org_Event";
            await calendarService.DeclineEventById(eventId);
        }

        [TestMethod]
        public async Task DeclineEventByIdTest_EventNotExist_Throws()
        {
            try
            {
                string eventId = "Get_Event_Not_Exist";
                await calendarService.DeclineEventById(eventId);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == MockGoogleServiceClient.EventIdNotFound);
                return;
            }

            Assert.Fail("Should throw exception");
        }
    }
}
