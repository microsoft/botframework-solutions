// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CalendarSkill.ServiceClients
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::CalendarSkill.Models;

    /// <summary>
    /// The calendar API interface.
    /// </summary>
    public interface ICalendarService
    {
        /// <summary>
        /// Create a event in user calendar with calendar info in newEvent.
        /// </summary>
        /// <param name="newEvent">event info to create.</param>
        /// <returns>the created meeting.</returns>
        Task<EventModel> CreateEvent(EventModel newEvent);

        /// <summary>
        /// Get the meetings that start time after now. Order by start time.
        /// </summary>
        /// <returns>the meetings list.</returns>
        Task<List<EventModel>> GetUpcomingEvents();

        /// <summary>
        /// Get the meetings that start time between the two parameters.
        /// </summary>
        /// <param name="startTime">meeting start time should be after the startTime.</param>
        /// <param name="endTime">meeting start time should be before the endTime.</param>
        /// <returns>the meetings list.</returns>
        Task<List<EventModel>> GetEventsByTime(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Get the meetings that start at the startTime.
        /// </summary>
        /// <param name="startTime">meeting start time.</param>
        /// <returns>the meetings list.</returns>
        Task<List<EventModel>> GetEventsByStartTime(DateTime startTime);

        /// <summary>
        /// Get the meetings with the specified words in the title.
        /// </summary>
        /// <param name="title">the meeting title should contains the title.</param>
        /// <returns>the meetings list.</returns>
        Task<List<EventModel>> GetEventsByTitle(string title);

        /// <summary>
        /// Update the meeting info.
        /// </summary>
        /// <param name="updateEvent">the new meeting info.</param>
        /// <returns>the updated meeting.</returns>
        Task<EventModel> UpdateEventById(EventModel updateEvent);

        /// <summary>
        /// Delete the meeting by ID.
        /// </summary>
        /// <param name="id">the meeting ID.</param>
        /// <returns>complete task.</returns>
        Task DeleteEventById(string id);

        /// <summary>
        /// Decline the meeting by ID.
        /// </summary>
        /// <param name="id">the meeting ID.</param>
        /// <returns>complete task.</returns>
        Task DeclineEventById(string id);

        /// <summary>
        /// Accept the meeting by ID.
        /// </summary>
        /// <param name="id">the meeting ID.</param>
        /// <returns>complete task.</returns>
        Task AcceptEventById(string id);
    }
}
