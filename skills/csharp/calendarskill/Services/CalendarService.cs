// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using Google.Apis.Calendar.v3.Data;

namespace CalendarSkill.Services
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
        public async Task<EventModel> CreateEventAysnc(EventModel newEvent)
        {
            return await calendarAPI.CreateEventAysnc(newEvent);
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetUpcomingEventsAsync(TimeSpan? timeSpan = null)
        {
            return await calendarAPI.GetUpcomingEventsAsync(timeSpan);
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByTimeAsync(DateTime startTime, DateTime endTime)
        {
            if (startTime.Kind != DateTimeKind.Utc)
            {
                throw new Exception("Get Event By Time -  Start Time is not UTC");
            }

            if (endTime.Kind != DateTimeKind.Utc)
            {
                throw new Exception("Get Event By Time -  End Time is not UTC");
            }

            return await calendarAPI.GetEventsByTimeAsync(startTime, endTime);
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByStartTimeAsync(DateTime startTime)
        {
            if (startTime.Kind != DateTimeKind.Utc)
            {
                throw new Exception("Get Event By Start Time -  Start Time is not UTC");
            }

            return await calendarAPI.GetEventsByStartTimeAsync(startTime);
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByTitleAsync(string title)
        {
            return await calendarAPI.GetEventsByTitleAsync(title);
        }

        /// <inheritdoc/>
        public async Task<EventModel> UpdateEventByIdAsync(EventModel updateEvent)
        {
            return await calendarAPI.UpdateEventByIdAsync(updateEvent);
        }

        /// <inheritdoc/>
        public async Task DeleteEventByIdAsync(string id)
        {
            await calendarAPI.DeleteEventByIdAsync(id);
            return;
        }

        public async Task DeclineEventByIdAsync(string id)
        {
            await calendarAPI.DeclineEventByIdAsync(id);
        }

        public async Task AcceptEventByIdAsync(string id)
        {
            await calendarAPI.AcceptEventByIdAsync(id);
        }

        public async Task<AvailabilityResult> GetUserAvailabilityAsync(string userEmail, List<string> users, DateTime startTime, int availabilityViewInterval)
        {
            return await calendarAPI.GetUserAvailabilityAsync(userEmail, users, startTime, availabilityViewInterval);
        }

        public async Task<List<bool>> CheckAvailable(List<string> users, DateTime startTime, int availabilityViewInterval)
        {
            return await calendarAPI.CheckAvailable(users, startTime, availabilityViewInterval);
        }
    }
}
