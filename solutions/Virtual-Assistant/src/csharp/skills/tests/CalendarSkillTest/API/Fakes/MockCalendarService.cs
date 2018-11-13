using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill;

namespace CalendarSkillTest.API.Fakes
{
    public class MockCalendarService : ICalendar
    {
        private readonly string token;

        public MockCalendarService(string token)
        {
            this.token = token;
        }

        public async Task<EventModel> CreateEvent(EventModel newEvent)
        {
            return await Task.FromResult(newEvent);
        }

        public Task DeleteEventById(string id)
        {
            return Task.CompletedTask;
        }

        public async Task<List<EventModel>> GetEventsByStartTime(DateTime startTime)
        {
            return await Task.FromResult(this.GetFakeEventModelList());
        }

        public async Task<List<EventModel>> GetEventsByTime(DateTime startTime, DateTime endTime)
        {
            return await Task.FromResult(this.GetFakeEventModelList());
        }

        public async Task<List<EventModel>> GetEventsByTitle(string title)
        {
            return await Task.FromResult(this.GetFakeEventModelList());
        }

        public async Task<List<EventModel>> GetUpcomingEvents()
        {
            return await Task.FromResult(this.GetFakeEventModelList());
        }

        public async Task<EventModel> UpdateEventById(EventModel updateEvent)
        {
            updateEvent.StartTime = DateTime.SpecifyKind(updateEvent.StartTime, DateTimeKind.Utc);
            updateEvent.EndTime = DateTime.SpecifyKind(updateEvent.EndTime, DateTimeKind.Utc);
            return await Task.FromResult(updateEvent);
        }

        public List<EventModel> GetFakeEventModelList()
        {
            List<EventModel> result = new List<EventModel>();

            var startTime = DateTime.SpecifyKind(new DateTime(), DateTimeKind.Utc);
            EventModel item = new EventModel(EventSource.Microsoft)
            {
                Id = "test",
                Title = "test title",
                Content = "test content",
                Attendees = new List<EventModel.Attendee>
                {
                    new EventModel.Attendee
                    {
                        Address = "test@test.com",
                        DisplayName = "test attendee",
                    },
                },
                StartTime = startTime,
                EndTime = startTime.AddHours(1),
                TimeZone = TimeZoneInfo.Local,
                Location = "test location",
                IsOrganizer = true,
            };
            result.Add(item);
            return result;
        }
    }
}
