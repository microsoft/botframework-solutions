﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;

namespace CalendarSkill
{
    public interface IServiceManager
    {
        IUserService InitUserService(string token, TimeZoneInfo info);

        ICalendar InitCalendarService(string token, EventSource source);
    }
}