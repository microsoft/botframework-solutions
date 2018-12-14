﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using Microsoft.Graph;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockCalendarService : ICalendar
    {
        public MockCalendarService(List<EventModel> fakeEventModels)
        {
            this.FakeEvents = fakeEventModels ?? new List<EventModel>();
        }

        public List<EventModel> FakeEvents { get; set; }

        public async Task<EventModel> CreateEvent(EventModel newEvent)
        {
            return await Task.FromResult(newEvent);
        }

        public async Task<List<EventModel>> GetUpcomingEvents()
        {
            return await Task.FromResult(this.FakeEvents);
        }

        public async Task<List<EventModel>> GetEventsByTime(DateTime startTime, DateTime endTime)
        {
            return await Task.FromResult(this.FakeEvents);
        }

        public async Task<List<EventModel>> GetEventsByStartTime(DateTime startTime)
        {
            return await Task.FromResult(this.FakeEvents);
        }

        public async Task<List<EventModel>> GetEventsByTitle(string title)
        {
            return await Task.FromResult(this.FakeEvents);
        }

        public async Task<EventModel> UpdateEventById(EventModel updateEvent)
        {
            updateEvent.StartTime = DateTime.SpecifyKind(new DateTime(2018, 11, 9, 9, 0, 0), DateTimeKind.Utc);
            updateEvent.EndTime = DateTime.SpecifyKind(new DateTime(2018, 11, 9, 10, 0, 0), DateTimeKind.Utc);
            return await Task.FromResult(updateEvent);
        }

        public async Task DeleteEventById(string id)
        {
            await Task.CompletedTask;
        }

        public static List<EventModel> FakeDefaultEvents()
        {
            var eventList = new List<EventModel>();

            eventList.Add(CreateEventModel());

            return eventList;
        }

        public static EventModel CreateEventModel(
            EmailAddress[] emailAddress = null,
            string eventName = null,
            string content = null,
            DateTime? startDateTime = null,
            DateTime? endDateTime = null,
            string locationString = null,
            bool isOrganizer = true,
            bool isCancelled = false)
        {
            var attendees = new List<Attendee>();

            if (emailAddress != null)
            {
                foreach (var email in emailAddress)
                {
                    attendees.Add(new Attendee
                    {
                        EmailAddress = email,
                        Type = AttendeeType.Required,
                    });
                }
            }
            else
            {
                attendees.Add(new Attendee
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = Strings.Strings.DefaultUserEmail,
                        Name = Strings.Strings.DefaultUserName,
                    },
                    Type = AttendeeType.Required,
                });
            }

            // Event Name
            eventName = eventName ?? Strings.Strings.DefaultEventName;

            // Event body
            var body = new ItemBody
            {
                Content = content ?? Strings.Strings.DefaultContent,
                ContentType = BodyType.Text,
            };

            // Event start and end time
            // Another example date format: `new DateTime(2017, 12, 1, 9, 30, 0).ToString("o")`
            if (startDateTime == null)
            {
                startDateTime = DateTime.UtcNow;
            }

            if (endDateTime == null)
            {
                endDateTime = startDateTime.Value.AddHours(1);
            }

            var startTimeTimeZone = new DateTimeTimeZone
            {
                DateTime = startDateTime.Value.ToString("o"),
                TimeZone = TimeZoneInfo.Local.Id,
            };
            var endTimeTimeZone = new DateTimeTimeZone
            {
                DateTime = endDateTime.Value.ToString("o"),
                TimeZone = TimeZoneInfo.Local.Id,
            };

            // Event location
            var location = new Location
            {
                DisplayName = locationString ?? Strings.Strings.DefaultLocation,
            };

            // Add the event.
            // await _graphClient.Me.Events.Request().AddAsync
            var createdEvent = new Event
            {
                Subject = eventName,
                Location = location,
                Attendees = attendees,
                Body = body,
                Start = startTimeTimeZone,
                End = endTimeTimeZone,
                IsOrganizer = isOrganizer,
                IsCancelled = isCancelled,
            };

            return new EventModel(createdEvent);
        }
    }
}
