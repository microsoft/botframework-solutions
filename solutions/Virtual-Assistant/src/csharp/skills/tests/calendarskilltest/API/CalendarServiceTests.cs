using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using CalendarSkillTest.API.Fakes;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalendarSkillTest.API
{
    [TestClass]
    public class CalendarServiceTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
        }

        [TestMethod]
        public async Task CreateEventTest()
        {
            EventModel newEvent = new EventModel(EventSource.Microsoft);

            ICalendar calendarService = new MockCalendarService("test token");

            await calendarService.CreateEvent(newEvent);
        }

        [TestMethod]
        public async Task GetUpComingEventsTest()
        {
            ICalendar calendarService = new MockCalendarService("test token");

            await calendarService.GetUpcomingEvents();
        }

        [TestMethod]
        public async Task GetEventsByTimeTest()
        {
            ICalendar calendarService = new MockCalendarService("test token");

            await calendarService.GetEventsByTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc), DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc));
        }

        [TestMethod]
        public async Task GetEventsByStartTimeTest()
        {
            ICalendar calendarService = new MockCalendarService("test token");

            await calendarService.GetEventsByStartTime(DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc));
        }

        [TestMethod]
        public async Task GetEventsByTitle()
        {
            ICalendar calendarService = new MockCalendarService("test token");

            await calendarService.GetEventsByTitle("test");
        }

        [TestMethod]
        public async Task DeleteEventsById()
        {
            ICalendar calendarService = new MockCalendarService("test token");

            await calendarService.DeleteEventById("test id");
        }

        [TestMethod]
        public async Task UpdateEventsById()
        {
            ICalendar calendarService = new MockCalendarService("test token");

            EventModel eventModel = new EventModel(EventSource.Microsoft);
            eventModel.Id = "test";
            eventModel.TimeZone = TimeZoneInfo.Utc;
            eventModel.StartTime = DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc);
            eventModel.EndTime = DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc);

            await calendarService.UpdateEventById(eventModel);
        }
    }
}
