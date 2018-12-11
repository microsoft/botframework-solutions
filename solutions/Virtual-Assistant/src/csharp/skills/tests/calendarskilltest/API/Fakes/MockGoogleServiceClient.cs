using System;
using System.Collections.Generic;
using System.Text;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Moq;
using GoogleCalendarService = Google.Apis.Calendar.v3.CalendarService;

namespace CalendarSkillTest.API.Fakes
{
    public static class MockGoogleServiceClient
    {
        public static Mock<GoogleCalendarService> mockCalendarService;
        public static Mock<EventsResource> mockEventsResource;

        static MockGoogleServiceClient()
        {
            mockCalendarService = new Mock<GoogleCalendarService>();
            mockEventsResource = new Mock<EventsResource>(mockCalendarService.Object);
            mockCalendarService.SetupGet(service => service.Events).Returns(mockEventsResource.Object);
            mockEventsResource.Setup(events => events.Insert(It.IsAny<Event>(), It.IsAny<string>())).Returns((Event body, string calendarId) =>
            {
                if (calendarId != "primary")
                {
                    throw new Exception("Calendar ID not support");
                }

                MockEventsResource.MockInsertRequest mockInsertRequest = new MockEventsResource.MockInsertRequest(mockCalendarService.Object, body, calendarId);

                return mockInsertRequest;
            });

            mockEventsResource.Setup(events => events.Patch(It.IsAny<Event>(), It.IsAny<string>(), It.IsAny<string>())).Returns((Event body, string calendarId, string eventId) =>
            {
                if (calendarId != "primary")
                {
                    throw new Exception("Calendar ID not support");
                }

                if (body.Id != eventId)
                {
                    throw new Exception("ID not match");
                }

                MockEventsResource.MockPatchRequest mockPatchRequest = new MockEventsResource.MockPatchRequest(mockCalendarService.Object, body, calendarId, eventId);
                //Mock<IClientServiceRequest<Event>> mockClientServiceRequest = new Mock<IClientServiceRequest<Event>>();

                return mockPatchRequest;
            });

            mockEventsResource.Setup(events => events.List(It.IsAny<string>())).Returns((string calendarId) =>
            {
                if (calendarId != "primary")
                {
                    throw new Exception("Calendar ID not support");
                }

                MockEventsResource.MockListRequest mockListRequest = new MockEventsResource.MockListRequest(mockCalendarService.Object, calendarId);

                return mockListRequest;
            });

            mockEventsResource.Setup(events => events.Delete(It.IsAny<string>(), It.IsAny<string>())).Returns((string calendarId, string eventId) =>
            {
                if (calendarId != "primary")
                {
                    throw new Exception("Calendar ID not support");
                }

                MockEventsResource.MockDeleteRequest mockDeleteRequest = new MockEventsResource.MockDeleteRequest(mockCalendarService.Object, calendarId, eventId);

                return mockDeleteRequest;
            });
        }

        public class MockEventsResource
        {
            public class MockInsertRequest : EventsResource.InsertRequest, IClientServiceRequest<Event>
            {
                public MockInsertRequest(IClientService service, Event body, string calendarId) : base(service, body, calendarId)
                {
                    this.Body = body;
                }

                public Event Body { get; set; }

                public new Event Execute()
                {
                    if (CalendarId != "primary")
                    {
                        throw new Exception("Calendar ID not support");
                    }

                    return this.Body;
                }
            }

            public class MockPatchRequest : EventsResource.PatchRequest, IClientServiceRequest<Event>
            {
                public MockPatchRequest(IClientService service, Event body, string calendarId, string eventId) : base(service, body, calendarId, eventId)
                {
                    this.Body = body;
                }

                public Event Body { get; set; }

                public new Event Execute()
                {
                    if (CalendarId != "primary")
                    {
                        throw new Exception("Calendar ID not support");
                    }

                    if (Body.Id != EventId)
                    {
                        throw new Exception("ID not match");
                    }

                    return this.Body;
                }
            }

            public class MockListRequest : EventsResource.ListRequest, IClientServiceRequest<Events>
            {
                private IList<Event> BuildInEvents;

                public MockListRequest(IClientService service, string calendarId) : base(service, calendarId)
                {
                    BuildInEvents = new List<Event>();

                    // add event datas
                    // common data prepare
                    string Location = "test_location";
                    IList<EventAttendee> Attendees = new List<EventAttendee>();
                    Attendees.Add(new EventAttendee()
                    {
                        Email = "test@gmail.com",
                        DisplayName = "Test Attendee"
                    });
                    string Timezone = "Etc/UTC";

                    // add start at same time
                    Event startAtSameTime = new Event();
                    startAtSameTime.Id = "0-0";
                    startAtSameTime.Summary = "start_at_same_time_0";
                    startAtSameTime.Description = "start at same time 0";
                    startAtSameTime.Start = new EventDateTime();
                    startAtSameTime.Start.TimeZone = Timezone;
                    startAtSameTime.Start.DateTimeRaw = "2500-01-01T18:00:00.0000000Z";
                    startAtSameTime.End = new EventDateTime();
                    startAtSameTime.End.TimeZone = Timezone;
                    startAtSameTime.End.DateTimeRaw = "2500-01-01T18:30:00.0000000Z";
                    startAtSameTime.Location = Location;
                    startAtSameTime.Attendees = Attendees;
                    startAtSameTime.Status = "confirmed";
                    BuildInEvents.Add(startAtSameTime);

                    startAtSameTime = new Event();
                    startAtSameTime.Id = "0-1";
                    startAtSameTime.Summary = "start_at_same_time_1";
                    startAtSameTime.Description = "start at same time 1";
                    startAtSameTime.Start = new EventDateTime();
                    startAtSameTime.Start.TimeZone = Timezone;
                    startAtSameTime.Start.DateTimeRaw = "2500-01-01T18:00:00.0000000Z";
                    startAtSameTime.End = new EventDateTime();
                    startAtSameTime.End.TimeZone = Timezone;
                    startAtSameTime.End.DateTimeRaw = "2500-01-01T18:30:00.0000000Z";
                    startAtSameTime.Location = Location;
                    startAtSameTime.Attendees = Attendees;
                    startAtSameTime.Status = "confirmed";
                    BuildInEvents.Add(startAtSameTime);

                    // add same name events
                    Event sameNameEvent = new Event();
                    sameNameEvent.Id = "1-0";
                    sameNameEvent.Summary = "same_name_event";
                    sameNameEvent.Description = "same name evene 0";
                    sameNameEvent.Start = new EventDateTime();
                    sameNameEvent.Start.TimeZone = Timezone;
                    sameNameEvent.Start.DateTimeRaw = "2500-01-01T19:00:00.0000000Z";
                    sameNameEvent.End = new EventDateTime();
                    sameNameEvent.End.TimeZone = Timezone;
                    sameNameEvent.End.DateTimeRaw = "2500-01-01T19:30:00.0000000Z";
                    sameNameEvent.Location = Location;
                    sameNameEvent.Attendees = Attendees;
                    sameNameEvent.Status = "confirmed";
                    BuildInEvents.Add(sameNameEvent);

                    sameNameEvent = new Event();
                    sameNameEvent.Id = "1-1";
                    sameNameEvent.Summary = "same_name_event";
                    sameNameEvent.Description = "same name evene 1";
                    sameNameEvent.Start = new EventDateTime();
                    sameNameEvent.Start.TimeZone = Timezone;
                    sameNameEvent.Start.DateTimeRaw = "2500-01-01T20:00:00.0000000Z";
                    sameNameEvent.End = new EventDateTime();
                    sameNameEvent.End.TimeZone = Timezone;
                    sameNameEvent.End.DateTimeRaw = "2500-01-01T20:30:00.0000000Z";
                    sameNameEvent.Location = Location;
                    sameNameEvent.Attendees = Attendees;
                    sameNameEvent.Status = "confirmed";
                    BuildInEvents.Add(sameNameEvent);

                    MaxResults = -1;
                }

                public new Events Execute()
                {
                    if (CalendarId != "primary")
                    {
                        throw new Exception("Calendar ID not support");
                    }

                    if (ShowDeleted.Value)
                    {
                        throw new Exception("Should not show deleted events");
                    }

                    if (!SingleEvents.Value)
                    {
                        throw new Exception("Not support none single events for now");
                    }

                    Events googleEvents = new Events();
                    IList<Event> events = new List<Event>();
                    foreach (Event anevent in this.BuildInEvents)
                    {
                        DateTime start = TimeZoneInfo.ConvertTimeToUtc(anevent.Start.DateTime.Value);
                        if ((TimeMin == null || start >= TimeMin) && (TimeMax == null || start <= TimeMax))
                        {
                            events.Add(anevent);
                        }

                        if (MaxResults > 0 && events.Count >= MaxResults)
                        {
                            break;
                        }
                    }

                    googleEvents.Items = events;
                    return googleEvents;
                }
            }

            public class MockDeleteRequest : EventsResource.DeleteRequest, IClientServiceRequest<string>
            {
                public MockDeleteRequest(IClientService service, string calendarId, string eventId) : base(service, calendarId, eventId)
                {
                }

                public new string Execute()
                {
                    if (CalendarId != "primary")
                    {
                        throw new Exception("Calendar ID not support");
                    }

                    if (EventId != "delete_event")
                    {
                        throw new Exception("Event id not found");
                    }

                    return this.EventId;
                }
            }
        }
    }
}
