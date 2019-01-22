// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using GoogleCalendarService = Google.Apis.Calendar.v3.CalendarService;

namespace CalendarSkill.ServiceClients.GoogleAPI
{
    /// <summary>
    /// The Google Calendar API service.
    /// </summary>
    public class GoogleCalendarAPI : ICalendarService
    {
        // the calendar id only used in google api, so set the const here.
        private const string CalendarId = "primary";
        private readonly GoogleCalendarService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleCalendarAPI"/> class.
        /// </summary>
        /// <param name="googleCalendarService">GoogleClient. </param>
        public GoogleCalendarAPI(GoogleCalendarService googleCalendarService)
        {
            _service = googleCalendarService;
        }

        public static GoogleCalendarService GetServiceClient(GoogleClient config, string token)
        {
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = config.ClientId,
                    ClientSecret = config.ClientSecret,
                },
                Scopes = config.Scopes,
                DataStore = new FileDataStore("Store"),
            });

            var tokenRes = new TokenResponse
            {
                AccessToken = token,
                ExpiresInSeconds = 3600,
                IssuedUtc = DateTime.UtcNow,
            };

            var credential = new UserCredential(flow, Environment.UserName, tokenRes);

            var calendarService = new GoogleCalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = config.ApplicationName,
            });

            return calendarService;
        }

        /// <inheritdoc/>
        public async Task<EventModel> CreateEvent(EventModel newEvent)
        {
            await Task.CompletedTask;
            return new EventModel(CreateEvent(newEvent.Value));
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetUpcomingEvents()
        {
            var events = GetEvents();
            var results = new List<EventModel>();
            foreach (var gevent in events.Items)
            {
                results.Add(new EventModel(gevent));
            }

            await Task.CompletedTask;
            return results;
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByTime(DateTime startTime, DateTime endTime)
        {
            var events = RequestEventsByTime(startTime, endTime);
            var results = new List<EventModel>();
            foreach (var gevent in events.Items)
            {
                results.Add(new EventModel(gevent));
            }

            await Task.CompletedTask;
            return results;
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByStartTime(DateTime startTime)
        {
            var events = RequestEventsByStartTime(startTime);
            var results = new List<EventModel>();
            foreach (var gevent in events.Items)
            {
                EventModel eventModel = new EventModel(gevent);
                if (startTime.CompareTo(eventModel.StartTime) == 0)
                {
                    results.Add(eventModel);
                }
            }

            await Task.CompletedTask;
            return results;
        }

        /// <inheritdoc/>
        public async Task<List<EventModel>> GetEventsByTitle(string title)
        {
            var events = RequestEventsByStartTime(DateTime.UtcNow.AddDays(-1));
            var results = new List<EventModel>();
            foreach (var gevent in events.Items)
            {
                if (gevent.Summary.ToLower().Contains(title.ToLower()))
                {
                    results.Add(new EventModel(gevent));
                }
            }

            await Task.CompletedTask;
            return results;
        }

        /// <inheritdoc/>
        public async Task<EventModel> UpdateEventById(EventModel updateEvent)
        {
            await Task.CompletedTask;
            return new EventModel(UpdateEventById(updateEvent.Value));
        }

        /// <inheritdoc/>
        public async Task DeleteEventById(string id)
        {
            await Task.CompletedTask;
            var result = DeleteEvent(id);
            return;
        }

        public async Task DeclineEventById(string id)
        {
            DeclineEvent(id);
            await Task.CompletedTask;
            return;
        }

        public async Task AcceptEventById(string id)
        {
            AcceptEvent(id);
            await Task.CompletedTask;
            return;
        }

        private Event UpdateEventById(Event updateEvent)
        {
            try
            {
                var request = _service.Events.Patch(updateEvent, CalendarId, updateEvent.Id);
                var gevent = ((IClientServiceRequest<Event>)request).Execute();
                return gevent;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        private Events RequestEventsByTime(DateTime startTime, DateTime endTime)
        {
            try
            {
                // Define parameters of request.
                var request = _service.Events.List(CalendarId);
                request.TimeMin = startTime;
                request.TimeMax = endTime;
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                // List events.
                var events = ((IClientServiceRequest<Events>)request).Execute();
                return events;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        private Events RequestEventsByStartTime(DateTime startTime)
        {
            try
            {
                // Define parameters of request.
                var request = _service.Events.List(CalendarId);
                request.TimeMin = startTime;
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.MaxResults = 10;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                // List events.
                var events = ((IClientServiceRequest<Events>)request).Execute();
                return events;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        private Events GetEvents()
        {
            try
            {
                // Define parameters of request.
                var request = _service.Events.List(CalendarId);
                request.TimeMin = DateTime.UtcNow;
                request.ShowDeleted = false;
                request.SingleEvents = true;
                request.MaxResults = 10;
                request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

                // List events.
                var events = ((IClientServiceRequest<Events>)request).Execute();
                return events;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        private Event CreateEvent(Event newEvent)
        {
            try
            {
                var request = _service.Events.Insert(newEvent, CalendarId);
                var gevent = ((IClientServiceRequest<Event>)request).Execute();
                return gevent;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        private string DeleteEvent(string id)
        {
            try
            {
                var request = _service.Events.Delete(CalendarId, id);
                var result = ((IClientServiceRequest<string>)request).Execute();
                return result;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        private void DeclineEvent(string id)
        {
            try
            {
                var request = _service.Events.Get(CalendarId, id);
                var gevent = ((IClientServiceRequest<Event>)request).Execute();
                foreach (var attendee in gevent.Attendees)
                {
                    if (attendee.Self.HasValue && attendee.Self.Value)
                    {
                        attendee.ResponseStatus = GoogleAttendeeStatus.Declined;
                        break;
                    }
                }

                gevent = UpdateEventById(gevent);
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        private void AcceptEvent(string id)
        {
            try
            {
                var request = _service.Events.Get(CalendarId, id);
                var gevent = ((IClientServiceRequest<Event>)request).Execute();
                foreach (var attendee in gevent.Attendees)
                {
                    if (attendee.Self.HasValue && attendee.Self.Value)
                    {
                        attendee.ResponseStatus = GoogleAttendeeStatus.Accepted;
                        break;
                    }
                }

                gevent = UpdateEventById(gevent);
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }
    }
}
