using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using Moq;

namespace CalendarSkillTest.API.Fakes.MockMSGraphClient
{
    public static class MockMSGraphServiceClient
    {
        private static Mock<IGraphServiceClient> mockCalendarService;

        static MockMSGraphServiceClient()
        {
            mockCalendarService = new Mock<IGraphServiceClient>();
            mockCalendarService.Setup(client => client.Me.Events.Request().AddAsync(It.IsAny<Event>())).Returns((Event body) =>
            {
                return Task.FromResult(body);
            });
            mockCalendarService.Setup(client => client.Me.Calendar.CalendarView.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(() =>
            {
                ICalendarCalendarViewCollectionPage result = new CalendarCalendarViewCollectionPage();
                Event anevent = new Event
                {
                    Id = "0",
                    Subject = "test",
                    Body = new ItemBody() { Content = "test" },
                    Start = new DateTimeTimeZone() { DateTime = "2500-01-01T18:00:00.0000000Z", TimeZone = TimeZoneInfo.Utc.Id },
                    End = new DateTimeTimeZone() { DateTime = "2500-01-01T18:30:00.0000000Z", TimeZone = TimeZoneInfo.Utc.Id }
                };
                result.Add(anevent);
                return Task.FromResult(result);
            });
            mockCalendarService.Setup(client => client.Me.CalendarView.Request(It.IsAny<List<QueryOption>>()).GetAsync()).Returns(() =>
            {
                IUserCalendarViewCollectionPage result = new UserCalendarViewCollectionPage();
                Event anevent = new Event
                {
                    Id = "0",
                    Subject = "test",
                    Body = new ItemBody() { Content = "test" },
                    Start = new DateTimeTimeZone() { DateTime = "2500-01-01T18:00:00.0000000Z", TimeZone = TimeZoneInfo.Utc.Id },
                    End = new DateTimeTimeZone() { DateTime = "2500-01-01T18:30:00.0000000Z", TimeZone = TimeZoneInfo.Utc.Id }
                };
                result.Add(anevent);
                return Task.FromResult(result);
            });

            mockCalendarService.Setup(client => client.Me.Events[It.IsAny<string>()]).Returns((string eventId) =>
            {
                Mock<IEventRequestBuilder> requestBuilder = new Mock<IEventRequestBuilder>();
                requestBuilder.Setup(req => req.Request().DeleteAsync()).Returns(() =>
                {
                    if (eventId == "Test_Access_Denied")
                    {
                        throw new ServiceException(new Error() { Message = "erroraccessdenied" });
                    }

                    if (eventId != "delete_event")
                    {
                        throw new Exception("Event id not found");
                    }

                    return Task.FromResult(eventId);
                });

                requestBuilder.Setup(req => req.Request().UpdateAsync(It.IsAny<Event>())).Returns((Event body) =>
                {
                    if (eventId != body.Id)
                    {
                        throw new Exception("ID not match");
                    }

                    return Task.FromResult(body);
                });

                requestBuilder.Setup(req => req.Decline(It.IsNotNull<string>(), null)).Returns((string comment, bool? sendResponse) =>
                {
                    Mock<IEventDeclineRequestBuilder> declineRequestBuilder = new Mock<IEventDeclineRequestBuilder>();
                    declineRequestBuilder.Setup(builder => builder.Request(null).PostAsync()).Returns(() =>
                    {
                        if (eventId != "decline_event")
                        {
                            throw new Exception("Event id not found");
                        }

                        if (comment != "decline")
                        {
                            throw new Exception("decline comment not right");
                        }

                        return Task.CompletedTask;
                    });
                    return declineRequestBuilder.Object;
                });

                requestBuilder.Setup(req => req.Accept(It.IsNotNull<string>(), null)).Returns((string comment, bool? sendResponse) =>
                {
                    Mock<IEventAcceptRequestBuilder> acceptRequestBuilder = new Mock<IEventAcceptRequestBuilder>();
                    acceptRequestBuilder.Setup(builder => builder.Request(null).PostAsync()).Returns(() =>
                    {
                        if (eventId != "accept_event")
                        {
                            throw new Exception("Event id not found");
                        }

                        if (comment != "accept")
                        {
                            throw new Exception("accept comment not right");
                        }

                        return Task.CompletedTask;
                    });
                    return acceptRequestBuilder.Object;
                });

                return requestBuilder.Object;
            });
        }

        public static IGraphServiceClient GetCalendarService()
        {
            return mockCalendarService.Object;
        }
    }
}
