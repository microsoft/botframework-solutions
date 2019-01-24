// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;

namespace CalendarSkill.ServiceClients
{
    public class CalendarService : ICalendarService
    {
        private ICalendarService calendarAPI;

        public CalendarService()
        {
            // to get pass when serialize
        }

        public CalendarService(ICalendarService calendarAPI, EventSource source)
        {
            this.calendarAPI = calendarAPI ?? throw new Exception("calendarAPI is null");
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
            if (startTime.Kind != DateTimeKind.Utc)
            {
                throw new Exception("Get Event By Time -  Start Time is not UTC");
            }

            if (endTime.Kind != DateTimeKind.Utc)
            {
                throw new Exception("Get Event By Time -  End Time is not UTC");
            }

            return await calendarAPI.GetEventsByTime(startTime, endTime);
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByStartTime(DateTime startTime)
        {
            if (startTime.Kind != DateTimeKind.Utc)
            {
                throw new Exception("Get Event By Start Time -  Start Time is not UTC");
            }

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

        public async Task DeclineEventById(string id)
        {
            await calendarAPI.DeclineEventById(id);
        }

        public async Task AcceptEventById(string id)
        {
            await calendarAPI.AcceptEventById(id);
        }
    }
}
