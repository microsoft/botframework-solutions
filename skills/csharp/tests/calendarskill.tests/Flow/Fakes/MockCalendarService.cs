// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Services;
using Microsoft.Graph;

namespace CalendarSkill.Test.Flow.Fakes
{
    // Leave this mock service for GetEventPrompt test. The moq service has issue when serialize, so need instead by this.
    public class MockCalendarService : ICalendarService
    {
        public MockCalendarService(List<EventModel> fakeEventModels)
        {
            this.FakeEvents = fakeEventModels ?? new List<EventModel>();
        }

        public List<EventModel> FakeEvents { get; set; }

        public static List<EventModel> FakeDefaultEvents()
        {
            var eventList = new List<EventModel>
            {
                CreateEventModel()
            };

            return eventList;
        }

        public static List<EventModel> FakeMultipleEvents(int count)
        {
            var eventList = new List<EventModel>();

            for (var i = 0; i < count; i++)
            {
                eventList.Add(CreateEventModel());
            }

            return eventList;
        }

        public static List<EventModel> FakeMultipleNextEvents(int count)
        {
            var eventList = new List<EventModel>();
            var startDateTime = DateTime.UtcNow.AddHours(1);
            var endDateTime = startDateTime.AddHours(1);

            for (var i = 0; i < count; i++)
            {
                eventList.Add(CreateEventModel(
                    startDateTime: startDateTime,
                    endDateTime: endDateTime));
            }

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
                var now = DateTime.Now;
                var startTime = new DateTime(now.Year, now.Month, now.Day, 18, 0, 0);
                startTime = TimeZoneInfo.ConvertTimeToUtc(startTime);
                startDateTime = startTime.AddDays(1);
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
                ResponseStatus = new ResponseStatus() { Response = ResponseType.Organizer }
            };

            return new EventModel(createdEvent);
        }

        public async Task<EventModel> CreateEventAysnc(EventModel newEvent)
        {
            return await Task.FromResult(newEvent);
        }

        public async Task<List<EventModel>> GetUpcomingEventsAsync(TimeSpan? timeSpan = null)
        {
            return await Task.FromResult(this.FakeEvents);
        }

        public async Task<List<EventModel>> GetEventsByTimeAsync(DateTime startTime, DateTime endTime)
        {
            return await Task.FromResult(this.FakeEvents);
        }

        public async Task<List<EventModel>> GetEventsByStartTimeAsync(DateTime startTime)
        {
            return await Task.FromResult(this.FakeEvents);
        }

        public async Task<List<EventModel>> GetEventsByTitleAsync(string title)
        {
            return await Task.FromResult(this.FakeEvents);
        }

        public async Task<EventModel> UpdateEventByIdAsync(EventModel updateEvent)
        {
            updateEvent.StartTime = DateTime.SpecifyKind(new DateTime(2018, 11, 9, 9, 0, 0), DateTimeKind.Utc);
            updateEvent.EndTime = DateTime.SpecifyKind(new DateTime(2018, 11, 9, 10, 0, 0), DateTimeKind.Utc);
            return await Task.FromResult(updateEvent);
        }

        public async Task DeleteEventByIdAsync(string id)
        {
            await Task.CompletedTask;
        }

        public async Task DeclineEventByIdAsync(string id)
        {
            await Task.CompletedTask;
        }

        public async Task AcceptEventByIdAsync(string id)
        {
            await Task.CompletedTask;
        }

        public Task<AvailabilityResult> GetUserAvailabilityAsync(string userEmail, List<string> users, DateTime startTime, int availabilityViewInterval)
        {
            throw new NotImplementedException();
        }

        public Task<List<bool>> CheckAvailable(List<string> users, DateTime startTime, int availabilityViewInterval)
        {
            throw new NotImplementedException();
        }
    }
}
