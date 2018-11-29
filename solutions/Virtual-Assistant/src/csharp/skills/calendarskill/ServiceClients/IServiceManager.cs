// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.


namespace CalendarSkill
{
    public interface IServiceManager
    {
        IUserService InitUserService(string token, EventSource source);

        ICalendar InitCalendarService(string token, EventSource source);
    }
}