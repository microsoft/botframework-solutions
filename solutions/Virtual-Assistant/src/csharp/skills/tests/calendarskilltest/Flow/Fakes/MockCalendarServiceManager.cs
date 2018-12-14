using System;
using CalendarSkill;
using CalendarSkill.ServiceClients;
using Microsoft.Graph;
using System.Collections.Generic;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockCalendarServiceManager : IServiceManager
    {
        public MockCalendarService CalendarService { get; set; }

        public MockUserService UserService { get; set; }

        public void SetupCalendarService(List<EventModel> fakeEventList)
        {
            CalendarService = new MockCalendarService(fakeEventList);
        }

        public void SetupUserService(List<User> fakeUserList, List<Person> fakePersonList)
        {
            UserService = new MockUserService(fakeUserList, fakePersonList);
        }

        public ICalendar InitCalendarService(string token, EventSource source)
        {
            return CalendarService;
        }

        public IUserService InitUserService(string token, EventSource source)
        {
            return UserService;
        }
    }
}
