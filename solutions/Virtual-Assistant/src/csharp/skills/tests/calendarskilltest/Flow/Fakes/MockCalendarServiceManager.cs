using System;
using CalendarSkill;
using CalendarSkill.ServiceClients.GoogleAPI;
using Microsoft.Graph;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockCalendarServiceManager : IServiceManager
    {
        public ICalendar InitCalendarService(string token, EventSource source)
        {
            return new MockCalendarService();
        }

        public IUserService InitUserService(string token, EventSource source)
        {
            return new MockUserService();
        }
    }
}
