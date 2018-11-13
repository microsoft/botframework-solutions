// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.Graph;

namespace CalendarSkill
{
    public interface IServiceManager
    {
        IUserService InitUserService(string token, TimeZoneInfo info);

        IUserService InitUserService(IGraphServiceClient graphClient, TimeZoneInfo info);

        ICalendar InitCalendarService(string token, EventSource source);

        ICalendar InitCalendarService(ICalendar calendarAPI, EventSource source);
    }
}