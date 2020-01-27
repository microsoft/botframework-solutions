// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace CalendarSkill.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::CalendarSkill.Models;
    using Microsoft.Graph;

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
        Task<EventModel> CreateEventAysnc(EventModel newEvent);

        /// <summary>
        /// Get the meetings that start time after now. Order by start time.
        /// </summary>
        /// <param name="timeSpan">Timespan to get upcoming event within.</param>
        /// <returns>the meetings list.</returns>
        Task<List<EventModel>> GetUpcomingEventsAsync(TimeSpan? timeSpan = null);

        /// <summary>
        /// Get the meetings that start time between the two parameters.
        /// </summary>
        /// <param name="startTime">meeting start time should be after the startTime.</param>
        /// <param name="endTime">meeting start time should be before the endTime.</param>
        /// <returns>the meetings list.</returns>
        Task<List<EventModel>> GetEventsByTimeAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Get the meetings that start at the startTime.
        /// </summary>
        /// <param name="startTime">meeting start time.</param>
        /// <returns>the meetings list.</returns>
        Task<List<EventModel>> GetEventsByStartTimeAsync(DateTime startTime);

        /// <summary>
        /// Get the meetings with the specified words in the title.
        /// </summary>
        /// <param name="title">the meeting title should contains the title.</param>
        /// <returns>the meetings list.</returns>
        Task<List<EventModel>> GetEventsByTitleAsync(string title);

        /// <summary>
        /// Update the meeting info.
        /// </summary>
        /// <param name="updateEvent">the new meeting info.</param>
        /// <returns>the updated meeting.</returns>
        Task<EventModel> UpdateEventByIdAsync(EventModel updateEvent);

        /// <summary>
        /// Delete the meeting by ID.
        /// </summary>
        /// <param name="id">the meeting ID.</param>
        /// <returns>complete task.</returns>
        Task DeleteEventByIdAsync(string id);

        /// <summary>
        /// Decline the meeting by ID.
        /// </summary>
        /// <param name="id">the meeting ID.</param>
        /// <returns>complete task.</returns>
        Task DeclineEventByIdAsync(string id);

        /// <summary>
        /// Accept the meeting by ID.
        /// </summary>
        /// <param name="id">the meeting ID.</param>
        /// <returns>complete task.</returns>
        Task AcceptEventByIdAsync(string id);

        /// <summary>
        /// find the available time slot of user at the time.
        /// </summary>
        /// <param name="userEmail">the current user's Email.</param>
        /// <param name="users">the user need to check availability.</param>
        /// <param name="startTime">the start time of available time slot.</param>
        /// <param name="availabilityViewInterval">Represents the duration of a time slot in an availabilityView in the response. The default is 30 minutes, minimum is 5, maximum is 1440. Optional.</param>
        /// <returns>the user available time slot from start time.</returns>
        Task<AvailabilityResult> GetUserAvailabilityAsync(string userEmail, List<string> users, DateTime startTime, int availabilityViewInterval);

        /// <summary>
        /// Check the users availablity.
        /// </summary>
        /// <param name="users">the users.</param>
        /// <param name="startTime">the start time.</param>
        /// <param name="availabilityViewInterval">the availability View Interval.</param>
        /// <returns>complete task.</returns>
        Task<List<bool>> CheckAvailable(List<string> users, DateTime startTime, int availabilityViewInterval);

    }
}
