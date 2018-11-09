using System;
using CalendarSkill;
using Microsoft.Graph;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockCalendarServiceManager : IServiceManager
    {
        public ICalendar InitCalendarService(string token, EventSource source, TimeZoneInfo info)
        {
            return new MockCalendarService();
        }

        public ICalendar InitCalendarService(ICalendar calendarAPI, EventSource source, TimeZoneInfo info)
        {
            throw new NotImplementedException();
        }

        public IUserService InitUserService(string token, TimeZoneInfo timeZoneInfo)
        {
            return new MockUserService();
        }

        public IUserService InitUserService(IGraphServiceClient graphClient, TimeZoneInfo info)
        {
            return new MockUserService();
        }
    }
}
