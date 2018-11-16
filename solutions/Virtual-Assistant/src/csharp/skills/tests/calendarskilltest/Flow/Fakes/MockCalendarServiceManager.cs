using System;
using CalendarSkill;
using CalendarSkill.ServiceClients.GoogleAPI;
using Microsoft.Graph;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockCalendarServiceManager : IServiceManager
    {
        public ICalendar InitCalendarService(ICalendar calendarAPI, EventSource source)
        {
            return new MockCalendarService();
        }

        public IUserService InitUserService(IGraphServiceClient graphClient, TimeZoneInfo info)
        {
            return new MockUserService();
        }

        public GoogleClient GetGoogleClient()
        {
            return new GoogleClient();
        }
    }
}
