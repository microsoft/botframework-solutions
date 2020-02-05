// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Utilities;
using Microsoft.Graph;

namespace CalendarSkill.Services.MSGraphAPI
{
    public class MSGraphCalendarAPI : ICalendarService
    {
        private readonly IGraphServiceClient _graphClient;

        public MSGraphCalendarAPI(IGraphServiceClient serviceClient)
        {
            _graphClient = serviceClient;
        }

        /// <inheritdoc/>
        public async Task<EventModel> CreateEventAysnc(EventModel newEvent)
        {
            Event new_event = await CreateEvent(newEvent.Value);
            if (new_event == null)
            {
                return null;
            }

            return new EventModel(new_event);
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetUpcomingEventsAsync(TimeSpan? timeSpan = null)
        {
            var eventList = new List<EventModel>();
            var msftEvents = await GetMyUpcomingCalendarView(timeSpan);
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
        public async Task<List<EventModel>> GetEventsByTimeAsync(DateTime startTime, DateTime endTime)
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
        public async Task<List<EventModel>> GetEventsByStartTimeAsync(DateTime startTime)
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
        public async Task<List<EventModel>> GetEventsByTitleAsync(string title)
        {
            var result = new List<EventModel>();
            if (!string.IsNullOrEmpty(title))
            {
                var allEvents = await GetEventsByTimeAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(7));
                foreach (var item in allEvents)
                {
                    if (item.Title.ToLower().Contains(title.ToLower()))
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<EventModel> UpdateEventByIdAsync(EventModel updateEvent)
        {
            return new EventModel(await this.UpdateEvent(updateEvent.Value));
        }

        /// <inheritdoc/>
        public async Task DeleteEventByIdAsync(string id)
        {
            try
            {
                await _graphClient.Me.Events[id].Request().DeleteAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }
        }

        public async Task DeclineEventByIdAsync(string id)
        {
            try
            {
                await _graphClient.Me.Events[id].Decline("decline").Request().PostAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }
        }

        public async Task AcceptEventByIdAsync(string id)
        {
            try
            {
                await _graphClient.Me.Events[id].Accept("accept").Request().PostAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }
        }

        // Check the availability of people/rooms with corresponding emails on condition of startTime and duration.
        public async Task<List<bool>> CheckAvailable(List<string> users, DateTime startTime, int availabilityViewInterval)
        {
            try
            {
                List<bool> availability = new List<bool>();
                var schedules = users;

                var intervalStartTime = new DateTimeTimeZone
                {
                    DateTime = startTime.ToString(),
                    TimeZone = "UTC"
                };

                var intervalEndTime = new DateTimeTimeZone
                {
                    DateTime = startTime.AddDays(1).ToString(),
                    TimeZone = "UTC"
                };

                ICalendarGetScheduleCollectionPage collectionPage = await _graphClient.Me.Calendar
                    .GetSchedule(schedules, intervalEndTime, intervalStartTime, availabilityViewInterval)
                    .Request()
                    .PostAsync();

                // AvailabilityView[0] == '0' means available, while others mean not available.
                foreach (var page in collectionPage)
                {
                    // Can't find this room.
                    if (page.AvailabilityView == null)
                    {
                        availability.Add(false);
                    }

                    // AvailabilityViem is empty, should not get into this state.
                    if (page.AvailabilityView.Length == 0)
                    {
                        throw new Exception("There is no elements in AvailabilityView");
                    }

                    availability.Add(page.AvailabilityView.Length > 0 && page.AvailabilityView[0] == '0');
                }

                return availability;
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }
        }

        public async Task<AvailabilityResult> GetUserAvailabilityAsync(string userEmail, List<string> attendees, DateTime startTime, int availabilityViewInterval)
        {
            List<bool> availability = new List<bool>();
            attendees.Add(userEmail);
            var schedules = attendees;
            var endTime = startTime.AddDays(1);

            var intervalStartTime = new DateTimeTimeZone
            {
                DateTime = startTime.ToString(),
                TimeZone = "UTC"
            };

            var intervalEndTime = new DateTimeTimeZone
            {
                DateTime = endTime.ToString(),
                TimeZone = "UTC"
            };

            ICalendarGetScheduleCollectionPage collectionPage = await _graphClient.Me.Calendar
                .GetSchedule(schedules, intervalEndTime, intervalStartTime, availabilityViewInterval)
                .Request()
                .PostAsync();

            var result = new AvailabilityResult();

            // set non-working time as 3 in availability view
            foreach (var collection in collectionPage)
            {
                var availabilityView = collection.AvailabilityView.ToCharArray();
                if (collection.WorkingHours != null)
                {
                    var workingTimeZone = TimeZoneInfo.FindSystemTimeZoneById(collection.WorkingHours.TimeZone.Name);
                    var startTimeInWorkTimeZone = TimeConverter.ConvertUtcToUserTime(startTime, workingTimeZone);
                    var workingStartTimeTimeOfDay = collection.WorkingHours.StartTime;
                    var workingStartTime = new DateTime(
                        startTimeInWorkTimeZone.Year,
                        startTimeInWorkTimeZone.Month,
                        startTimeInWorkTimeZone.Day,
                        workingStartTimeTimeOfDay.Hour,
                        workingStartTimeTimeOfDay.Minute,
                        workingStartTimeTimeOfDay.Second);

                    var workingEndTimeTimeOfDay = collection.WorkingHours.EndTime;
                    var workingEndTime = new DateTime(
                        startTimeInWorkTimeZone.Year,
                        startTimeInWorkTimeZone.Month,
                        startTimeInWorkTimeZone.Day,
                        workingEndTimeTimeOfDay.Hour,
                        workingEndTimeTimeOfDay.Minute,
                        workingEndTimeTimeOfDay.Second);

                    var workDays = collection.WorkingHours.DaysOfWeek.Select(li => (int)li);
                    for (int i = 0; i < availabilityView.Length; i++)
                    {
                        if (availabilityView[i] == '0')
                        {
                            var availabilityViewStartTime = startTime.AddMinutes(i * CalendarCommonUtil.AvailabilityViewInterval);
                            availabilityViewStartTime = TimeConverter.ConvertUtcToUserTime(availabilityViewStartTime, workingTimeZone);

                            if (!workDays.Contains((int)availabilityViewStartTime.DayOfWeek))
                            {
                                availabilityView[i] = '3';
                                continue;
                            }

                            var availabilityViewEndTime = availabilityViewStartTime.AddMinutes(CalendarCommonUtil.AvailabilityViewInterval);

                            if (!((availabilityViewStartTime.TimeOfDay >= workingStartTime.TimeOfDay && availabilityViewStartTime.TimeOfDay < workingEndTime.TimeOfDay) ||
                                (availabilityViewEndTime.TimeOfDay > workingStartTime.TimeOfDay && availabilityViewEndTime.TimeOfDay <= workingEndTime.TimeOfDay)))
                            {
                                availabilityView[i] = '3';
                            }
                        }
                    }
                }

                result.AvailabilityViewList.Add(new string(availabilityView));
            }

            result.MySchedule.AddRange(collectionPage.Last().ScheduleItems.Select(li => new EventModel(EventSource.Microsoft)
            {
                Title = li.Subject,
                StartTime = DateTime.Parse(li.Start.DateTime + "Z").ToUniversalTime(),
                EndTime = DateTime.Parse(li.End.DateTime + "Z").ToUniversalTime(),
                Location = li.Location
            }));

            return result;
        }

        /// <summary>
        /// Update an event info.
        /// </summary>
        /// <param name="updateEvent">new event info.</param>
        /// <returns>The updated event.</returns>
        private async Task<Event> UpdateEvent(Event updateEvent)
        {
            try
            {
                var updatedEvet = await _graphClient.Me.Events[updateEvent.Id].Request().UpdateAsync(updateEvent);
                return updatedEvet;
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }
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

            IUserCalendarViewCollectionPage events = null;
            try
            {
                events = await _graphClient.Me.CalendarView.Request(options).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

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
        private async Task<List<Event>> GetMyUpcomingCalendarView(TimeSpan? timeSpan = null)
        {
            var items = new List<Event>();

            // Define the time span for the calendar view.
            var options = new List<QueryOption>
            {
                new QueryOption("startDateTime", DateTime.UtcNow.ToString("o")),
                new QueryOption("endDateTime", timeSpan == null ? DateTime.UtcNow.AddDays(1).ToString("o") : DateTime.UtcNow.Add(timeSpan.Value).ToString("o")),
                new QueryOption("$orderBy", "start/dateTime"),
            };

            ICalendarCalendarViewCollectionPage calendar = null;

            try
            {
                calendar = await _graphClient.Me.Calendar.CalendarView.Request(options).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

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

            IUserCalendarViewCollectionPage events = null;

            try
            {
                events = await _graphClient.Me.CalendarView.Request(options).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

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
            try
            {
                // Add the event.
                var createdEvent = await _graphClient.Me.Events.Request().AddAsync(newEvent);
                return createdEvent;
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }
        }

        private async Task<MeetingTimeSuggestionsResult> FindMeetingTimes(IEnumerable<AttendeeBase> attendees, TimeConstraint timeConstraint = null, bool isOrgnizerOptional = true)
        {
            try
            {
                var suggestion = await _graphClient.Me.FindMeetingTimes(attendees, timeConstraint: timeConstraint, isOrganizerOptional: isOrgnizerOptional).Request().PostAsync();
                return suggestion;
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }
        }
    }
}