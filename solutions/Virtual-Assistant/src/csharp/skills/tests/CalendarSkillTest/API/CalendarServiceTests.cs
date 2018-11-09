using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using CalendarSkillTest.API.Fakes;
using CalendarSkillTest.API.Fakes;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalendarSkillTest.API
{
    [TestClass]
    public class MailServiceTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
        }

        [TestMethod]
        public async Task CreateEventTest()
        {
            EventModel newEvent = new EventModel(EventSource.Microsoft);

            ICalendar mockCalendarService = new FakeCalendarService("test token");
            IServiceManager serviceManager = new ServiceManager();
            ICalendar calendarService = serviceManager.InitCalendarService(mockCalendarService, EventSource.Microsoft, TimeZoneInfo.Local);

            await calendarService.CreateEvent(newEvent);
        }

        [TestMethod]
        public async Task GetUpComingEventsTest()
        {
            ICalendar mockCalendarService = new FakeCalendarService("test token");
            IServiceManager serviceManager = new ServiceManager();
            ICalendar calendarService = serviceManager.InitCalendarService(mockCalendarService, EventSource.Microsoft, TimeZoneInfo.Local);

            await calendarService.GetUpcomingEvents();
        }

        [TestMethod]
        public async Task GetEventsByTimeTest()
        {
            ICalendar mockCalendarService = new FakeCalendarService("test token");
            IServiceManager serviceManager = new ServiceManager();
            ICalendar calendarService = serviceManager.InitCalendarService(mockCalendarService, EventSource.Microsoft, TimeZoneInfo.Local);

            await calendarService.GetEventsByTime(new DateTime(), new DateTime());
        }

        [TestMethod]
        public async Task GetEventsByStartTimeTest()
        {
            ICalendar mockCalendarService = new FakeCalendarService("test token");
            IServiceManager serviceManager = new ServiceManager();
            ICalendar calendarService = serviceManager.InitCalendarService(mockCalendarService, EventSource.Microsoft, TimeZoneInfo.Local);

            await calendarService.GetEventsByStartTime(new DateTime());
        }

        [TestMethod]
        public async Task GetEventsByTitle()
        {
            ICalendar mockCalendarService = new FakeCalendarService("test token");
            IServiceManager serviceManager = new ServiceManager();
            ICalendar calendarService = serviceManager.InitCalendarService(mockCalendarService, EventSource.Microsoft, TimeZoneInfo.Local);

            await calendarService.GetEventsByTitle("test");
        }

        [TestMethod]
        public async Task DeleteEventsById()
        {
            ICalendar mockCalendarService = new FakeCalendarService("test token");
            IServiceManager serviceManager = new ServiceManager();
            ICalendar calendarService = serviceManager.InitCalendarService(mockCalendarService, EventSource.Microsoft, TimeZoneInfo.Local);

            await calendarService.DeleteEventById("test id");
        }

        [TestMethod]
        public async Task UpdateEventsById()
        {
            ICalendar mockCalendarService = new FakeCalendarService("test token");
            IServiceManager serviceManager = new ServiceManager();
            ICalendar calendarService = serviceManager.InitCalendarService(mockCalendarService, EventSource.Microsoft, TimeZoneInfo.Local);

            EventModel eventModel = new EventModel(EventSource.Microsoft);
            eventModel.StartTime = new DateTime();
            eventModel.EndTime = new DateTime();

            await calendarService.UpdateEventById(eventModel);
        }
    }
}
