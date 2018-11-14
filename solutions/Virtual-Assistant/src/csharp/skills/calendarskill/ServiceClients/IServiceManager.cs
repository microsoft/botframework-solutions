// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using CalendarSkill.ServiceClients.GoogleAPI;
using Microsoft.Graph;

namespace CalendarSkill
{
    public interface IServiceManager
    {
        IUserService InitUserService(IGraphServiceClient graphClient, TimeZoneInfo info);

        ICalendar InitCalendarService(ICalendar calendarAPI, EventSource source);

        GoogleClient GetGoogleClient();
    }
}