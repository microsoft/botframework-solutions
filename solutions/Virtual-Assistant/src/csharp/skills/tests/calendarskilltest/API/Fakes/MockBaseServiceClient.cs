using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using Moq;

namespace CalendarSkillTest.API.Fakes
{
    public static class MockBaseServiceClient
    {
        public static Mock<ICalendar> mockCalendarService;

        static MockBaseServiceClient()
        {
            mockCalendarService = new Mock<ICalendar>();
            mockCalendarService.Setup(service => service.CreateEvent(It.IsAny<EventModel>())).Returns((EventModel body) => Task.FromResult(body));
            mockCalendarService.Setup(service => service.GetUpcomingEvents()).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.GetEventsByTime(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.GetEventsByStartTime(It.IsAny<DateTime>())).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.GetEventsByTitle(It.IsAny<string>())).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.UpdateEventById(It.IsAny<EventModel>())).Returns((EventModel body) => Task.FromResult(body));
            mockCalendarService.Setup(service => service.DeleteEventById(It.IsAny<string>())).Returns(Task.CompletedTask);
        }
    }
}
