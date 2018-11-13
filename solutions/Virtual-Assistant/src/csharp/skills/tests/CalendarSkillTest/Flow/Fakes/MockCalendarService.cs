using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;
using Microsoft.Graph;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockCalendarService : ICalendar
    {
        public MockCalendarService()
        {
            this.UpcomingEvents = FakeGetUpcomingEvents();
        }

        public List<EventModel> UpcomingEvents { get; set; }

        public async Task<EventModel> CreateEvent(EventModel newEvent)
        {
            return await Task.FromResult(newEvent);
        }

        public async Task<List<EventModel>> GetUpcomingEvents()
        {
            return await Task.FromResult(this.UpcomingEvents);
        }

        public async Task<List<EventModel>> GetEventsByTime(DateTime startTime, DateTime endTime)
        {
            return await Task.FromResult(this.UpcomingEvents);
        }

        public async Task<List<EventModel>> GetEventsByStartTime(DateTime startTime)
        {
            return await Task.FromResult(this.UpcomingEvents);
        }

        public async Task<List<EventModel>> GetEventsByTitle(string title)
        {
            return await Task.FromResult(this.UpcomingEvents);
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

        private List<EventModel> FakeGetUpcomingEvents()
        {
            var eventList = new List<EventModel>();
            var attendees = new List<Attendee>();

            attendees.Add(new Attendee
            {
                EmailAddress = new EmailAddress
                {
                    Address = "test1@outlook.com",
                },
                Type = AttendeeType.Required,
            });

            // Event Name
            string eventName = "test title";

            // Event body
            var body = new ItemBody
            {
                Content = "test body",
                ContentType = BodyType.Text,
            };

            // Event start and end time
            // Another example date format: `new DateTime(2017, 12, 1, 9, 30, 0).ToString("o")`
            var startTimeTimeZone = new DateTimeTimeZone
            {
                DateTime = new DateTime(2019, 11, 11, 9, 30, 0).ToString("o"),
                TimeZone = TimeZoneInfo.Local.Id,
            };
            var endTimeTimeZone = new DateTimeTimeZone
            {
                DateTime = new DateTime(2019, 11, 11, 10, 30, 0).ToString("o"),
                TimeZone = TimeZoneInfo.Local.Id,
            };

            // Event location
            var location = new Location
            {
                DisplayName = "office 12",
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
                IsOrganizer = true,
            };

            EventModel createdEventModel = new EventModel(createdEvent);
            eventList.Add(createdEventModel);

            return eventList;
        }
    }
}
