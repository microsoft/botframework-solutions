using System;
using System.Collections.Generic;
using System.Text;
using Google;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Moq;
using GoogleCalendarService = Google.Apis.Calendar.v3.CalendarService;

namespace CalendarSkillTest.API.Fakes.MockGoogleClient
{
    public static class MockGoogleServiceClient
    {
        public const string CalendarIdNotSupported = "Calendar ID not supported";
        public const string EventIdNotFound = "Event id not found";
        private static Mock<GoogleCalendarService> mockCalendarService;
        private static Mock<EventsResource> mockEventsResource;

        static MockGoogleServiceClient()
        {
            mockCalendarService = new Mock<GoogleCalendarService>();
            mockEventsResource = new Mock<EventsResource>(mockCalendarService.Object);
            mockCalendarService.SetupGet(service => service.Events).Returns(mockEventsResource.Object);
            mockEventsResource.Setup(events => events.Insert(It.IsAny<Event>(), It.IsAny<string>())).Returns((Event body, string calendarId) =>
            {
                if (calendarId != "primary")
                {
                    throw new Exception(CalendarIdNotSupported);
                }

                MockEventsResource.MockInsertRequest mockInsertRequest = new MockEventsResource.MockInsertRequest(mockCalendarService.Object, body, calendarId);

                return mockInsertRequest;
            });

            mockEventsResource.Setup(events => events.Patch(It.IsAny<Event>(), It.IsAny<string>(), It.IsAny<string>())).Returns((Event body, string calendarId, string eventId) =>
            {
                if (calendarId != "primary")
                {
                    throw new Exception(CalendarIdNotSupported);
                }

                if (body.Id != eventId)
                {
                    throw new Exception("ID not match");
                }

                MockEventsResource.MockPatchRequest mockPatchRequest = new MockEventsResource.MockPatchRequest(mockCalendarService.Object, body, calendarId, eventId);

                return mockPatchRequest;
            });

            mockEventsResource.Setup(events => events.List(It.IsAny<string>())).Returns((string calendarId) =>
            {
                if (calendarId != "primary")
                {
                    throw new Exception(CalendarIdNotSupported);
                }

                MockEventsResource.MockListRequest mockListRequest = new MockEventsResource.MockListRequest(mockCalendarService.Object, calendarId);

                return mockListRequest;
            });

            mockEventsResource.Setup(events => events.Delete(It.IsAny<string>(), It.IsAny<string>())).Returns((string calendarId, string eventId) =>
            {
                if (calendarId != "primary")
                {
                    throw new Exception(CalendarIdNotSupported);
                }

                MockEventsResource.MockDeleteRequest mockDeleteRequest = new MockEventsResource.MockDeleteRequest(mockCalendarService.Object, calendarId, eventId);

                return mockDeleteRequest;
            });

            mockEventsResource.Setup(events => events.Get(It.IsAny<string>(), It.IsAny<string>())).Returns((string calendarId, string eventId) =>
            {
                if (calendarId != "primary")
                {
                    throw new Exception(CalendarIdNotSupported);
                }

                MockEventsResource.MockGetRequest mockGetRequest = new MockEventsResource.MockGetRequest(mockCalendarService.Object, calendarId, eventId);

                return mockGetRequest;
            });
        }

        public static GoogleCalendarService GetCalendarService()
        {
            return mockCalendarService.Object;
        }

        public class MockEventsResource
        {
            public class MockInsertRequest : EventsResource.InsertRequest, IClientServiceRequest<Event>
            {
                public MockInsertRequest(IClientService service, Event body, string calendarId)
                    : base(service, body, calendarId)
                {
                    this.Body = body;
                }

                public Event Body { get; set; }

                public new Event Execute()
                {
                    if (CalendarId != "primary")
                    {
                        throw new Exception(CalendarIdNotSupported);
                    }

                    return this.Body;
                }
            }

            public class MockPatchRequest : EventsResource.PatchRequest, IClientServiceRequest<Event>
            {
                public MockPatchRequest(IClientService service, Event body, string calendarId, string eventId)
                    : base(service, body, calendarId, eventId)
                {
                    this.Body = body;
                }

                public Event Body { get; set; }

                public new Event Execute()
                {
                    if (CalendarId != "primary")
                    {
                        throw new Exception(CalendarIdNotSupported);
                    }

                    if (Body.Id != EventId)
                    {
                        throw new Exception("ID not match");
                    }

                    return this.Body;
                }
            }

            public class MockListRequest : EventsResource.ListRequest, IClientServiceRequest<Events> // To make excute work.
            {
                // using some data to test
                // todo:
                // Use data file instead, make test better
                private IList<Event> buildInEvents;

                public MockListRequest(IClientService service, string calendarId)
                    : base(service, calendarId)
                {
                    buildInEvents = new List<Event>();

                    // add event datas
                    // common data prepare
                    string location = "test_location";
                    IList<EventAttendee> attendees = new List<EventAttendee>
                    {
                        new EventAttendee()
                        {
                            Email = "test@gmail.com",
                            DisplayName = "Test Attendee"
                        }
                    };
                    string timezone = "Etc/UTC";

                    // add start at same time
                    Event startAtSameTime = new Event
                    {
                        Id = "0-0",
                        Summary = "start_at_same_time_0",
                        Description = "start at same time 0",
                        Start = new EventDateTime
                        {
                            TimeZone = timezone,
                            DateTimeRaw = "2500-01-01T18:00:00.0000000Z"
                        },
                        End = new EventDateTime
                        {
                            TimeZone = timezone,
                            DateTimeRaw = "2500-01-01T18:30:00.0000000Z"
                        },
                        Location = location,
                        Attendees = attendees,
                        Status = "confirmed"
                    };
                    buildInEvents.Add(startAtSameTime);

                    startAtSameTime = new Event
                    {
                        Id = "0-1",
                        Summary = "start_at_same_time_1",
                        Description = "start at same time 1",
                        Start = new EventDateTime
                        {
                            TimeZone = timezone,
                            DateTimeRaw = "2500-01-01T18:00:00.0000000Z"
                        },
                        End = new EventDateTime
                        {
                            TimeZone = timezone,
                            DateTimeRaw = "2500-01-01T18:30:00.0000000Z"
                        },
                        Location = location,
                        Attendees = attendees,
                        Status = "confirmed"
                    };
                    buildInEvents.Add(startAtSameTime);

                    // add same name events
                    Event sameNameEvent = new Event
                    {
                        Id = "1-0",
                        Summary = "same_name_event",
                        Description = "same name evene 0",
                        Start = new EventDateTime
                        {
                            TimeZone = timezone,
                            DateTimeRaw = "2500-01-01T19:00:00.0000000Z"
                        },
                        End = new EventDateTime
                        {
                            TimeZone = timezone,
                            DateTimeRaw = "2500-01-01T19:30:00.0000000Z"
                        },
                        Location = location,
                        Attendees = attendees,
                        Status = "confirmed"
                    };
                    buildInEvents.Add(sameNameEvent);

                    sameNameEvent = new Event
                    {
                        Id = "1-1",
                        Summary = "same_name_event",
                        Description = "same name evene 1",
                        Start = new EventDateTime
                        {
                            TimeZone = timezone,
                            DateTimeRaw = "2500-01-01T20:00:00.0000000Z"
                        },
                        End = new EventDateTime
                        {
                            TimeZone = timezone,
                            DateTimeRaw = "2500-01-01T20:30:00.0000000Z"
                        },
                        Location = location,
                        Attendees = attendees,
                        Status = "confirmed"
                    };
                    buildInEvents.Add(sameNameEvent);

                    MaxResults = -1;
                }

                public new Events Execute()
                {
                    if (CalendarId != "primary")
                    {
                        throw new Exception(CalendarIdNotSupported);
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
                    foreach (Event anevent in this.buildInEvents)
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
                public MockDeleteRequest(IClientService service, string calendarId, string eventId)
                    : base(service, calendarId, eventId)
                {
                }

                public new string Execute()
                {
                    if (CalendarId != "primary")
                    {
                        throw new Exception(CalendarIdNotSupported);
                    }

                    if (EventId == "Test_Access_Denied")
                    {
                        var exception = new GoogleApiException("Delete", "insufficient permission")
                        {
                            Error = new RequestError
                            {
                                Message = "insufficient permission"
                            }
                        };
                        throw exception;
                    }

                    // todo:
                    // will add test for id not exist.
                    // for now "delete_event" is set as the only legal id.
                    // we can change this to real data set in future and make not found logic real.
                    if (EventId != "delete_event")
                    {
                        throw new Exception(EventIdNotFound);
                    }

                    return this.EventId;
                }
            }

            public class MockGetRequest : EventsResource.GetRequest, IClientServiceRequest<Event> // To make excute work.
            {
                // using some data to test
                // todo:
                // Use data file instead, make test better
                private Event buildInEvent;

                public MockGetRequest(IClientService service, string calendarId, string eventId)
                    : base(service, calendarId, eventId)
                {
                    string location = "test_location";
                    IList<EventAttendee> attendees = new List<EventAttendee>
                    {
                        new EventAttendee()
                        {
                            Email = "test@gmail.com",
                            DisplayName = "Test Attendee",
                            Self = true,
                            ResponseStatus = "needsAction",
                        }
                    };
                    string timezone = "Etc/UTC";

                    buildInEvent = new Event
                    {
                        Id = "Get_Not_Org_Event",
                        Summary = "NotOrganizerMeeting",
                        Description = "user is not the organizer of this meeting",
                        Start = new EventDateTime
                        {
                            TimeZone = timezone,
                            DateTimeRaw = "2500-01-01T18:00:00.0000000Z"
                        },
                        End = new EventDateTime
                        {
                            TimeZone = timezone,
                            DateTimeRaw = "2500-01-01T18:30:00.0000000Z"
                        },
                        Location = location,
                        Attendees = attendees,
                        Status = "confirmed",
                        Organizer = new Event.OrganizerData
                        {
                            Email = "test_organizer@gmail.com",
                            DisplayName = "Test Organizer",
                            Self = null,
                        },
                    };
                }

                public new Event Execute()
                {
                    if (CalendarId != "primary")
                    {
                        throw new Exception(CalendarIdNotSupported);
                    }

                    if (EventId != buildInEvent.Id)
                    {
                        throw new Exception(EventIdNotFound);
                    }

                    return buildInEvent;
                }
            }
        }
    }
}
