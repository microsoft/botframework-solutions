// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkillTest.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using CalendarSkill;

    public class FakeCalendarServiceManager : IServiceManager
    {
        public IUserService InitUserService(string token, TimeZoneInfo info)
        {
            return new FakeUserService(token);
        }

        public ICalendar InitCalendarService(string token, EventSource source, TimeZoneInfo info)
        {
            return new FakeCalendarService(token);
        }
    }
}