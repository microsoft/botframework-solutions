using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using CalendarSkill.ServiceClients;
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
        public static ICalendar calendarService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            calendarService = new CalendarService(MockBaseServiceClient.mockCalendarService.Object, EventSource.Microsoft);
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
        public async Task GetEventsByStartTimeTest()
        {
            await calendarService.GetEventsByStartTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc));
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
            EventModel updateEvent = new EventModel(EventSource.Microsoft);
            updateEvent.Id = "update_event";
            updateEvent.TimeZone = TimeZoneInfo.Utc;
            updateEvent.StartTime = DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc);
            updateEvent.EndTime = DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc);

            EventModel updateResult = await calendarService.UpdateEventById(updateEvent);
            Assert.IsTrue(updateEvent.Id == updateResult.Id);

        }
    }

    // this will test all logic in MSGraph Calendar service
    // only have success test now
    // todo: add more error test
    [TestClass]
    public class MSGrapghCalendarServiceTests
    {
        public static ICalendar calendarService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            calendarService = new CalendarService(new MSGraphCalendarAPI(MockMSGraphServiceClient.mockCalendarService.Object), EventSource.Microsoft);
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
            DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T18:00:00.0000000Z"));
            DateTime endTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T19:00:00.0000000Z"));
            List<EventModel> events = await calendarService.GetEventsByTime(startTime, endTime);
            Assert.IsTrue(events.Count == 1);
        }

        [TestMethod]
        public async Task GetEventsByStartTimeTest()
        {
            DateTime startTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Parse("2500-01-01T18:00:00.0000000Z"));
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
            EventModel updateEvent = new EventModel(new Microsoft.Graph.Event());
            updateEvent.Id = "update_event";
            EventModel updateResult = await calendarService.UpdateEventById(updateEvent);
            Assert.IsTrue(updateEvent.Id == updateResult.Id);
        }

        [TestMethod]
        public async Task DeleteEventByIdTest()
        {
            string deleteId = "delete_event";
            await calendarService.DeleteEventById(deleteId);
        }
    }

    // this will test all logic in Google Calendar service
    // only have success test now
    // todo: add more error test
    [TestClass]
    public class GoogleCalendarServiceTests
    {
        public static ICalendar calendarService;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            calendarService = new CalendarService(new GoogleCalendarAPI(MockGoogleServiceClient.mockCalendarService.Object), EventSource.Google);
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
            EventModel updateEvent = new EventModel(EventSource.Google);
            updateEvent.Id = "update_event";
            EventModel updateResult = await calendarService.UpdateEventById(updateEvent);
            Assert.IsTrue(updateEvent.Id == updateResult.Id);
        }

        [TestMethod]
        public async Task DeleteEventByIdTest()
        {
            string deleteId = "delete_event";
            await calendarService.DeleteEventById(deleteId);
        }
    }
}
