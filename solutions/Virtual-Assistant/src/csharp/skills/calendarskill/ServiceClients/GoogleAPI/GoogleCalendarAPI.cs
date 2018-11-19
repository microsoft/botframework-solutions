// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace CalendarSkill.ServiceClients.GoogleAPI
{
    /// <summary>
    /// The Google Calendar API service.
    /// </summary>
    public class GoogleCalendarAPI : ICalendar
    {
        private static Google.Apis.Calendar.v3.CalendarService service;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleCalendarAPI"/> class.
        /// </summary>
        /// <param name="config">GoogleClient. </param>
        /// <param name="token">access token. </param>
        public GoogleCalendarAPI(GoogleClient config, string token)
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

            service = new Google.Apis.Calendar.v3.CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = config.ApplicationName,
            });
        }

        /// <inheritdoc/>
        public async Task<EventModel> CreateEvent(EventModel newEvent)
        {
            await Task.CompletedTask;
            return new EventModel(await CreateEvent(newEvent.Value));
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

        private Event UpdateEventById(Event updateEvent)
        {
            var request = service.Events.Patch(updateEvent, "primary", updateEvent.Id);
            var gevent = request.Execute();
            return gevent;
        }

        private Events RequestEventsByTime(DateTime startTime, DateTime endTime)
        {
            // Define parameters of request.
            var request = service.Events.List("primary");
            request.TimeMin = startTime;
            request.TimeMax = endTime;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            var events = request.Execute();
            return events;
        }

        private Events RequestEventsByStartTime(DateTime startTime)
        {
            // Define parameters of request.
            var request = service.Events.List("primary");
            request.TimeMin = startTime;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            var events = request.Execute();
            return events;
        }

        private Events GetEvents()
        {
            // Define parameters of request.
            var request = service.Events.List("primary");
            request.TimeMin = DateTime.UtcNow;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.MaxResults = 10;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            // List events.
            var events = request.Execute();
            return events;
        }

        private Event GetNextEvent()
        {
            var events = GetEvents();
            if (events == null || events.Items == null || events.Items.Count <= 0)
            {
                return null;
            }

            return events.Items[0];
        }

        private Event CreateEvent(Event newEvent)
        {
            return service.Events.Insert(newEvent, "primary").Execute();
        }

        private string DeleteEvent(string id)
        {
            var request = service.Events.Delete("primary", id);
            var result = request.Execute();
            return result;
        }
    }
}
