// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CalendarSkill.Models;

namespace CalendarSkill.Services
{
    public interface IServiceManager
    {
        IUserService InitUserService(string token, EventSource source);

        ICalendarService InitCalendarService(string token, EventSource source);
    }
}