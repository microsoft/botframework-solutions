// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace CalendarSkill
{
    public class MSGraphCalendarAPI : ICalendar
    {
        private readonly IGraphServiceClient _graphClient;

        public MSGraphCalendarAPI(IGraphServiceClient serviceClient)
        {
            _graphClient = serviceClient;
        }

        /// <inheritdoc/>
        public async Task<EventModel> CreateEvent(EventModel newEvent)
        {
            Event new_event = await CreateEvent(newEvent.Value);
            if (new_event == null)
            {
                return null;
            }

            return new EventModel(new_event);
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetUpcomingEvents()
        {
            var eventList = new List<EventModel>();
            var msftEvents = await GetMyUpcomingCalendarView();
            foreach (var msftEvent in msftEvents)
            {
                var newEvent = new EventModel(msftEvent);
                if (TimeZoneInfo.ConvertTimeToUtc(newEvent.StartTime, newEvent.TimeZone) >= DateTime.UtcNow)
                {
                    eventList.Add(newEvent);
                }
            }

            return eventList;
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByTime(DateTime startTime, DateTime endTime)
        {
            var eventList = new List<EventModel>();
            var msftEvents = await GetMyCalendarViewByTime(startTime, endTime);
            foreach (var msftEvent in msftEvents)
            {
                eventList.Add(new EventModel(msftEvent));
            }

            return eventList;
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByStartTime(DateTime startTime)
        {
            var allEvents = await GetMyStartTimeEvents(startTime);
            var result = new List<EventModel>();

            foreach (var item in allEvents)
            {
                var modelItem = new EventModel(item);
                if (modelItem.StartTime.CompareTo(startTime) == 0)
                {
                    result.Add(modelItem);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByTitle(string title)
        {
            var allEvents = await GetMyStartTimeEvents(DateTime.UtcNow.AddDays(-1));
            var result = new List<EventModel>();

            foreach (var item in allEvents)
            {
                var modelItem = new EventModel(item);
                if (modelItem.Title.ToLower().Contains(title.ToLower()))
                {
                    result.Add(modelItem);
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<EventModel> UpdateEventById(EventModel updateEvent)
        {
            return new EventModel(await this.UpdateEvent(updateEvent.Value));
        }

        /// <inheritdoc/>
        public async Task DeleteEventById(string id)
        {
            try
            {
                await _graphClient.Me.Events[id].Request().DeleteAsync();
            }
            catch (Exception e)
            {
                throw e;
            }

            return;
        }

        /// <summary>
        /// Accept a meeting request.
        /// </summary>
        /// <param name="id">the meeting id.</param>
        /// <returns>the meetings list.</returns>
        public async Task<List<Event>> AcceptMeetingRequest(string id)
        {
            var items = new List<Event>();

            // This snippet first checks whether the selected event originates with an invitation from the current user. If it did,
            // the SDK would throw an ErrorInvalidRequest exception because organizers can't accept their own invitations.
            var msftEvent = await _graphClient.Me.Events[id].Request().Select("ResponseStatus").GetAsync();
            if (msftEvent.ResponseStatus.Response != ResponseType.Organizer)
            {
                // Accept the meeting.
                await _graphClient.Me.Events[id].Accept("accept").Request().PostAsync();

                // no return value
            }
            else
            {
                // todo: Let the user know the operation isn't supported for this event.
            }

            return items;
        }

        /// <summary>
        /// Decline a meeting request.
        /// </summary>
        /// <param name="id">the meeting id.</param>
        /// <returns>the meetings list.</returns>
        public async Task<List<Event>> DeclineMeetingRequest(string id)
        {
            var items = new List<Event>();

            // This snippet first checks whether the selected event originates with an invitation from the current user. If it did,
            // the SDK would throw an ErrorInvalidRequest exception because organizers can't accept their own invitations.
            var msftEvent = await _graphClient.Me.Events[id].Request().Select("ResponseStatus").GetAsync();
            if (msftEvent.ResponseStatus.Response != ResponseType.Organizer)
            {
                // Accept the meeting.
                await _graphClient.Me.Events[id].Decline("decline").Request().PostAsync();

                // no return value
            }
            else
            {
                // todo: Let the user know the operation isn't supported for this event.
            }

            return items;
        }

        /// <summary>
        /// Update an event info.
        /// </summary>
        /// <param name="updateEvent">new event info.</param>
        /// <returns>The updated event.</returns>
        public async Task<Event> UpdateEvent(Event updateEvent)
        {
            var updatedEvet = await _graphClient.Me.Events[updateEvent.Id].Request().UpdateAsync(updateEvent);
            return updatedEvet;
        }

        // Get events in all the current user's mail folders.
        private async Task<List<Event>> GetMyEvents()
        {
            var items = new List<Event>();

            // Get events.
            var events = await _graphClient.Me.Events.Request().GetAsync();

            if (events?.Count > 0)
            {
                items.AddRange(events);
            }

            return items;
        }

        // Get events in all the current user's mail folders.
        private async Task<List<Event>> GetMyStartTimeEvents(DateTime startTime)
        {
            var items = new List<Event>();

            // Get events.
            var options = new List<QueryOption>();
            var endTime = startTime.AddDays(3);
            options.Add(new QueryOption("startdatetime", startTime.ToString("o")));
            options.Add(new QueryOption("enddatetime", endTime.ToString("o")));
            options.Add(new QueryOption("$orderBy", "start/dateTime"));
            var events = await _graphClient.Me.CalendarView.Request(options).GetAsync();

            if (events?.Count > 0)
            {
                items.AddRange(events);
            }

            while (events.NextPageRequest != null)
            {
                events = await events.NextPageRequest.GetAsync();
                if (events?.Count > 0)
                {
                    items.AddRange(events);
                }
            }

            return items;
        }

        // Get user's calendar view.
        // This snippets gets events for the next seven days.
        private async Task<List<Event>> GetMyUpcomingCalendarView()
        {
            var items = new List<Event>();

            // Define the time span for the calendar view.
            var options = new List<QueryOption>
            {
                new QueryOption("startDateTime", DateTime.UtcNow.ToString("o")),
                new QueryOption("endDateTime", DateTime.UtcNow.AddDays(1).ToString("o")),
                new QueryOption("$orderBy", "start/dateTime"),
            };

            var calendar = await _graphClient.Me.Calendar.CalendarView.Request(options).GetAsync();
            if (calendar?.Count > 0)
            {
                items.AddRange(calendar);
            }

            while (calendar.NextPageRequest != null)
            {
                calendar = await calendar.NextPageRequest.GetAsync();
                if (calendar?.Count > 0)
                {
                    items.AddRange(calendar);
                }
            }

            return items;
        }

        private async Task<List<Event>> GetMyCalendarViewByTime(DateTime startTime, DateTime endTime)
        {
            var items = new List<Event>();

            // Define the time span for the calendar view.
            var options = new List<QueryOption>
            {
                new QueryOption("startDateTime", startTime.ToString("o")),
                new QueryOption("endDateTime", endTime.ToString("o")),
                new QueryOption("$orderBy", "start/dateTime"),
            };

            var events = await _graphClient.Me.CalendarView.Request(options).GetAsync();
            if (events?.Count > 0)
            {
                items.AddRange(events);
            }

            while (events.NextPageRequest != null)
            {
                events = await events.NextPageRequest.GetAsync();
                if (events?.Count > 0)
                {
                    items.AddRange(events);
                }
            }

            return items;
        }

        private async Task<Event> CreateEvent(Event newEvent)
        {
            // Add the event.
            var createdEvent = await _graphClient.Me.Events.Request().AddAsync(newEvent);
            return createdEvent;
        }

        // Get a specified event.
        private async Task<List<Event>> GetEvent(string id)
        {
            var items = new List<Event>();

            // Get the event.
            var retrievedEvent = await _graphClient.Me.Events[id].Request().GetAsync();

            if (retrievedEvent != null)
            {
                // Get event properties.
                items.Add(retrievedEvent);
            }

            return items;
        }
    }
}