// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using CalendarSkill.Models;

namespace CalendarSkill.ServiceClients
{
    public interface IServiceManager
    {
        IUserService InitUserService(string token, EventSource source);

        ICalendarService InitCalendarService(string token, EventSource source);
    }
}