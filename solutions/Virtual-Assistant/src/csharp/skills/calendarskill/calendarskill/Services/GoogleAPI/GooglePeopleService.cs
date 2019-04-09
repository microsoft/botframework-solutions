// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.People.v1;
using Google.Apis.People.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace CalendarSkill.Services.GoogleAPI
{
    /// <summary>
    /// The Google People API service.
    /// </summary>
    public class GooglePeopleService : IUserService
    {
        private static PeopleService service;

        private List<PersonModel> cachedPeople;

        /// <summary>
        /// Initializes a new instance of the <see cref="GooglePeopleService"/> class.
        /// </summary>
        /// <param name="peopleService">people service.</param>
        public GooglePeopleService(PeopleService peopleService)
        {
            service = peopleService;
            cachedPeople = null;
        }

        public static PeopleService GetServiceClient(GoogleClient config, string token)
        {
            GoogleAuthorizationCodeFlow flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = config.ClientId,
                    ClientSecret = config.ClientSecret,
                },
                Scopes = config.Scopes,
                DataStore = new FileDataStore("Store"),
            });

            TokenResponse tokenRes = new TokenResponse
            {
                AccessToken = token,
                ExpiresInSeconds = 3600,
                IssuedUtc = DateTime.UtcNow,
            };

            UserCredential credential = new UserCredential(flow, Environment.UserName, tokenRes);
            var service = new PeopleService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = config.ApplicationName,
            });

            return service;
        }

        // search people in domain
        public async Task<List<PersonModel>> GetPeopleAsync(string name)
        {
            try
            {
                List<PersonModel> persons = null;
                if (this.cachedPeople != null)
                {
                    persons = this.cachedPeople;
                }
                else
                {
                    persons = await GetGooglePeopleAsync();
                }

                List<PersonModel> result = new List<PersonModel>();
                foreach (var person in persons)
                {
                    // filter manually
                    var displayName = person.DisplayName;
                    if (person.Emails?.Count > 0 && displayName != null && displayName.ToLower().Contains(name.ToLower()))
                    {
                        result.Add(person);
                    }
                }

                this.cachedPeople = result;
                return result;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        // search people in domain
        public Task<List<PersonModel>> GetUserAsync(string name)
        {
            return Task.FromResult(new List<PersonModel>());
        }

        // To do: finish contact search
        public Task<List<PersonModel>> GetContactsAsync(string name)
        {
            return Task.FromResult(new List<PersonModel>());
        }

        public async Task<PersonModel> GetMeAsync()
        {
            try
            {
                var peopleRequest = service.People.Get("people/me");
                peopleRequest.RequestMaskIncludeField = "person.emailAddresses,person.names,person.photos";
                var me = await peopleRequest.ExecuteAsync();

                if (me != null)
                {
                    return new PersonModel(me);
                }

                return null;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        public async Task<string> GetPhotoAsync(string email)
        {
            List<PersonModel> persons = null;
            if (this.cachedPeople != null)
            {
                persons = this.cachedPeople;
            }
            else
            {
                persons = await GetGooglePeopleAsync();
            }

            foreach (var person in persons)
            {
                // filter manually
                var displayName = person.DisplayName;
                foreach (var personEmail in person.Emails)
                {
                    if (email.Equals(personEmail))
                    {
                        return person.Photo;
                    }
                }
            }

            return null;
        }

        // get people work with
        private async Task<List<PersonModel>> GetGooglePeopleAsync()
        {
            try
            {
                PeopleResource.ConnectionsResource.ListRequest peopleRequest = service.People.Connections.List("people/me");
                peopleRequest.RequestMaskIncludeField = "person.emailAddresses,person.names,person.photos";

                ListConnectionsResponse connectionsResponse = await ((IClientServiceRequest<ListConnectionsResponse>)peopleRequest).ExecuteAsync();
                IList<Person> connections = connectionsResponse.Connections;
                List<PersonModel> result = new List<PersonModel>();
                if (connections != null && connections.Count > 0)
                {
                    foreach (var people in connections)
                    {
                        if (people != null)
                        {
                            result.Add(new PersonModel(people));
                        }
                    }
                }

                return result;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }
    }
}
