using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using CalendarSkill.ServiceClients.GoogleAPI;
using CalendarSkill.ServiceClients.MSGraphAPI;
using CalendarSkillTest.API.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalendarSkillTest.API
{
    // this will test all logic in CalendarService only.
    // only have success test now
    // todo: add more error test
    [TestClass]
    public class BaseCalendarServiceTests
    {
        public static ICalendarService CalendarService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            CalendarService = new CalendarService(MockBaseServiceClient.GetCalendarService(), EventSource.Microsoft);
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
            await CalendarService.CreateEvent(newEvent);
        }

        [TestMethod]
        public async Task GetUpComingEventsTest()
        {
            await CalendarService.GetUpcomingEvents();
        }

        [TestMethod]
        public async Task GetEventsByTimeTest()
        {
            await CalendarService.GetEventsByTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc), DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc));
        }

        [TestMethod]
        public async Task GetEventsByTimeTest_StartTimeNotUtc_Throws()
        {
            try
            {
                await CalendarService.GetEventsByTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Local), DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc));
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
                await CalendarService.GetEventsByTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc), DateTime.SpecifyKind(new DateTime(), DateTimeKind.Local));
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
            await CalendarService.GetEventsByStartTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc));
        }

        [TestMethod]
        public async Task GetEventsByStartTimeTest_NotUtc_Throws()
        {
            try
            {
                await CalendarService.GetEventsByStartTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Local));
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
            await CalendarService.GetEventsByTitle("test");
        }

        [TestMethod]
        public async Task DeleteEventsById()
        {
            await CalendarService.DeleteEventById("test id");
        }

        [TestMethod]
        public async Task UpdateEventsById()
        {
            EventModel updateEvent = new EventModel(EventSource.Microsoft);
            updateEvent.Id = "update_event";
            updateEvent.TimeZone = TimeZoneInfo.Utc;
            updateEvent.StartTime = DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc);
            updateEvent.EndTime = DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc);

            EventModel updateResult = await CalendarService.UpdateEventById(updateEvent);
            Assert.IsTrue(updateEvent.Id == updateResult.Id);
        }
    }

    // this will test all logic in MSGraph Calendar service
    // only have success test now
    // todo: add more error test
    [TestClass]
    public class MSGrapghCalendarServiceTests
    {
        public static ICalendarService CalendarService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            CalendarService = new CalendarService(new MSGraphCalendarAPI(MockMSGraphServiceClient.GetCalendarService()), EventSource.Microsoft);
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
            EventModel createEvent = new EventModel(new Microsoft.Graph.Event());
            createEvent.Id = "create_event";
            EventModel createResult = await CalendarService.CreateEvent(createEvent);
            Assert.IsTrue(createEvent.Id == createResult.Id);
        }

        [TestMethod]
        public async Task GetUpcomingEventsTest()
        {
            List<EventModel> events = await CalendarService.GetUpcomingEvents();
            Assert.IsTrue(events.Count == 1);
        }

        [TestMethod]
        public async Task GetEventsByTimeTest()
        {
            DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T18:00:00.0000000Z"));
            DateTime endTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T19:00:00.0000000Z"));
            List<EventModel> events = await CalendarService.GetEventsByTime(startTime, endTime);
            Assert.IsTrue(events.Count == 1);
        }

        [TestMethod]
        public async Task GetEventsByStartTimeTest()
        {
            DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T18:00:00.0000000Z"));
            List<EventModel> events = await CalendarService.GetEventsByStartTime(startTime);
            Assert.IsTrue(events.Count == 1);
        }

        [TestMethod]
        public async Task GetEventsByTitleTest()
        {
            List<EventModel> events = await CalendarService.GetEventsByTitle("test");
            Assert.IsTrue(events.Count == 1);
        }

        [TestMethod]
        public async Task UpdateEventByIdTest()
        {
            EventModel updateEvent = new EventModel(new Microsoft.Graph.Event());
            updateEvent.Id = "update_event";
            EventModel updateResult = await CalendarService.UpdateEventById(updateEvent);
            Assert.IsTrue(updateEvent.Id == updateResult.Id);
        }

        [TestMethod]
        public async Task DeleteEventByIdTest()
        {
            string deleteId = "delete_event";
            await CalendarService.DeleteEventById(deleteId);
        }

        [TestMethod]
        public async Task DeleteEventByIdTest_EventNotExist_Throws()
        {
            try
            {
                string deleteId = "delete_not_exist_event";
                await CalendarService.DeleteEventById(deleteId);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message == "Event id not found");
                return;
            }

            Assert.Fail("Should throw exception");
        }
    }

    // this will test all logic in Google Calendar service
    // only have success test now
    // todo: add more error test
    [TestClass]
    public class GoogleCalendarServiceTests
    {
        public static ICalendarService CalendarService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            CalendarService = new CalendarService(new GoogleCalendarAPI(MockGoogleServiceClient.GetCalendarService()), EventSource.Google);
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
            EventModel createEvent = new EventModel(new Google.Apis.Calendar.v3.Data.Event());
            createEvent.Id = "create_event";
            EventModel createResult = await CalendarService.CreateEvent(createEvent);
            Assert.IsTrue(createEvent.Id == createResult.Id);
        }

        [TestMethod]
        public async Task GetUpcomingEventsTest()
        {
            List<EventModel> upcomingEvents = await CalendarService.GetUpcomingEvents();
            Assert.IsTrue(upcomingEvents.Count == 4);
        }

        [TestMethod]
        public async Task GetEventsByTimeTest()
        {
            DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T18:00:00.0000000Z"));
            DateTime endTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T19:00:00.0000000Z"));
            List<EventModel> events = await CalendarService.GetEventsByTime(startTime, endTime);
            Assert.IsTrue(events.Count == 3);
        }

        [TestMethod]
        public async Task GetEventsByStartTimeTest()
        {
            DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T18:00:00.0000000Z"));
            List<EventModel> events = await CalendarService.GetEventsByStartTime(startTime);
            Assert.IsTrue(events.Count == 2);
        }

        [TestMethod]
        public async Task GetEventsByTitleTest()
        {
            List<EventModel> events = await CalendarService.GetEventsByTitle("same_name_event");
            Assert.IsTrue(events.Count == 2);
        }

        [TestMethod]
        public async Task UpdateEventByIdTest()
        {
            EventModel updateEvent = new EventModel(EventSource.Google);
            updateEvent.Id = "update_event";
            EventModel updateResult = await CalendarService.UpdateEventById(updateEvent);
            Assert.IsTrue(updateEvent.Id == updateResult.Id);
        }

        [TestMethod]
        public async Task DeleteEventByIdTest()
        {
            string deleteId = "delete_event";
            await CalendarService.DeleteEventById(deleteId);
        }

        [TestMethod]
        public async Task DeleteEventByIdTest_EventNotExist_Throws()
        {
            try
            {
                string deleteId = "delete_not_exist_event";
                await CalendarService.DeleteEventById(deleteId);
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
