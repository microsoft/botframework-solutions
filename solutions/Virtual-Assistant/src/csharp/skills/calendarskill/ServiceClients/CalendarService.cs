// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalendarSkill
{
    public class CalendarService : ICalendar
    {
        private ICalendar calendarAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarService"/> class.
        /// </summary>
        /// <param name="token">the access token.</param>
        /// <param name="source">the calendar provider.</param>
        /// <param name="timeZoneInfo">the user timezone info.</param>
        public CalendarService(string token, EventSource source, TimeZoneInfo timeZoneInfo)
        {
            switch (source)
            {
                case EventSource.Microsoft:
                    calendarAPI = new MSGraphCalendarAPI(token, timeZoneInfo);
                    break;
                case EventSource.Google:
                    // Todo: Google API timezone?
                    calendarAPI = new GoogleCalendarAPI(token);
                    break;
                default:
                    throw new Exception("Event Type not Defined");
            }
        }

        public CalendarService(ICalendar calendarAPI, EventSource source, TimeZoneInfo timeZoneInfo)
        {
            switch (source)
            {
                case EventSource.Microsoft:
                    this.calendarAPI = calendarAPI;
                    break;
                case EventSource.Google:
                    // Todo: Google API timezone?
                    this.calendarAPI = calendarAPI;
                    break;
                default:
                    throw new Exception("Event Type not Defined");
            }
        }

        /// <inheritdoc/>
        public async Task<EventModel> CreateEvent(EventModel newEvent)
        {
            return await calendarAPI.CreateEvent(newEvent);
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetUpcomingEvents()
        {
            return await calendarAPI.GetUpcomingEvents();
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByTime(DateTime startTime, DateTime endTime)
        {
            return await calendarAPI.GetEventsByTime(startTime, endTime);
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByStartTime(DateTime startTime)
        {
            return await calendarAPI.GetEventsByStartTime(startTime);
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByTitle(string title)
        {
            return await calendarAPI.GetEventsByTitle(title);
        }

        /// <inheritdoc/>
        public async Task<EventModel> UpdateEventById(EventModel updateEvent)
        {
            return await calendarAPI.UpdateEventById(updateEvent);
        }

        /// <inheritdoc/>
        public async Task DeleteEventById(string id)
        {
            await calendarAPI.DeleteEventById(id);
            return;
        }
    }
}
