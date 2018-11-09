// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.Graph;

namespace CalendarSkill
{
    public class ServiceManager : IServiceManager
    {
        /// <summary>
        /// Init user service with access token.
        /// </summary>
        /// <param name="token">access token.</param>
        /// <param name="info">user timezone info.</param>
        /// <returns>user service.</returns>
        public IUserService InitUserService(string token, TimeZoneInfo info)
        {
            return new MSGraphUserService(token, info);
        }

        public IUserService InitUserService(IGraphServiceClient graphClient, TimeZoneInfo info)
        {
            return new MSGraphUserService(graphClient, info);
        }

        /// <inheritdoc/>
        public ICalendar InitCalendarService(string token, EventSource source, TimeZoneInfo info)
        {
            return new CalendarService(token, source, info);
        }

        public ICalendar InitCalendarService(ICalendar calendarAPI, EventSource source, TimeZoneInfo info)
        {
            return new CalendarService(calendarAPI, source, info);
        }
    }
}
