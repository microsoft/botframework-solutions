using System;
using CalendarSkill;
using CalendarSkill.ServiceClients.GoogleAPI;
using Microsoft.Graph;
using System.Collections.Generic;

namespace CalendarSkillTest.Flow.Fakes
{
    public class MockCalendarServiceManager : IServiceManager
    {
        public List<EventModel> FakeEventList { get; set; }

        public List<User> FakeUserList { get; set; }

        public List<Person> FakePeopleList { get; set; }

        public ICalendar InitCalendarService(string token, EventSource source)
        {
            return new MockCalendarService(FakeEventList);
        }

        public IUserService InitUserService(string token, EventSource source)
        {
            return new MockUserService(FakeUserList, FakePeopleList);
        }
    }
}
