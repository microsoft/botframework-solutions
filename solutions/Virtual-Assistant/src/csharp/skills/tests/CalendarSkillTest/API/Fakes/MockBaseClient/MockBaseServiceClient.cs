using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.ServiceClients;
using Moq;

namespace CalendarSkillTest.API.Fakes.MockBaseClient
{
    public static class MockBaseServiceClient
    {
        private static Mock<ICalendarService> mockCalendarService;

        static MockBaseServiceClient()
        {
            mockCalendarService = new Mock<ICalendarService>();
            mockCalendarService.Setup(service => service.CreateEvent(It.IsAny<EventModel>())).Returns((EventModel body) => Task.FromResult(body));
            mockCalendarService.Setup(service => service.GetUpcomingEvents()).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.GetEventsByTime(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.GetEventsByStartTime(It.IsAny<DateTime>())).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.GetEventsByTitle(It.IsAny<string>())).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.UpdateEventById(It.IsAny<EventModel>())).Returns((EventModel body) => Task.FromResult(body));
            mockCalendarService.Setup(service => service.DeleteEventById(It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        public static ICalendarService GetCalendarService()
        {
            return mockCalendarService.Object;
        }
    }
}
