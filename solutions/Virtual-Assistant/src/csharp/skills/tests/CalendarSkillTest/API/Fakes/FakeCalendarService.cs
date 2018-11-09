using CalendarSkill;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalendarSkillTest.API.Fakes
{
    public class FakeCalendarService : ICalendar
    {
        private readonly string token;

        public FakeCalendarService(string token)
        {
            this.token = token;
        }

        public async Task<EventModel> CreateEvent(EventModel newEvent)
        {
            return newEvent;
        }

        public Task DeleteEventById(string id)
        {
            return Task.CompletedTask;
        }

        public async Task<List<EventModel>> GetEventsByStartTime(DateTime startTime)
        {
            return this.GetFakeEventModelList();
        }

        public async Task<List<EventModel>> GetEventsByTime(DateTime startTime, DateTime endTime)
        {
            return this.GetFakeEventModelList();
        }

        public async Task<List<EventModel>> GetEventsByTitle(string title)
        {
            return this.GetFakeEventModelList();
        }

        public async Task<List<EventModel>> GetUpcomingEvents()
        {
            return this.GetFakeEventModelList();
        }

        public async Task<EventModel> UpdateEventById(EventModel updateEvent)
        {
            updateEvent.StartTime = DateTime.SpecifyKind(updateEvent.StartTime, DateTimeKind.Unspecified);
            updateEvent.EndTime = DateTime.SpecifyKind(updateEvent.EndTime, DateTimeKind.Unspecified);
            return updateEvent;
        }

        public List<EventModel> GetFakeEventModelList()
        {
            List<EventModel> result = new List<EventModel>();

            var startTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
            EventModel item = new EventModel(EventSource.Microsoft)
            {
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
