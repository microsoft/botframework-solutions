// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Services;
using Moq;

namespace CalendarSkill.Test.API.Fakes.MockBaseClient
{
    public static class MockBaseServiceClient
    {
        private static Mock<ICalendarService> mockCalendarService;

        static MockBaseServiceClient()
        {
            mockCalendarService = new Mock<ICalendarService>();
            mockCalendarService.Setup(service => service.CreateEventAysnc(It.IsAny<EventModel>())).Returns((EventModel body) => Task.FromResult(body));
            mockCalendarService.Setup(service => service.GetUpcomingEventsAsync(null)).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.GetEventsByTimeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.GetEventsByStartTimeAsync(It.IsAny<DateTime>())).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.GetEventsByTitleAsync(It.IsAny<string>())).Returns(Task.FromResult(new List<EventModel>()));
            mockCalendarService.Setup(service => service.UpdateEventByIdAsync(It.IsAny<EventModel>())).Returns((EventModel body) => Task.FromResult(body));
            mockCalendarService.Setup(service => service.DeleteEventByIdAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        }

        public static ICalendarService GetCalendarService()
        {
            return mockCalendarService.Object;
        }
    }
}
