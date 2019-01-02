// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.People.v1;
using Google.Apis.People.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Graph;
using GooglePerson = Google.Apis.People.v1.Data.Person;
using MsPerson = Microsoft.Graph.Person;

namespace EmailSkill.ServiceClients.GoogleAPI
{
    /// <summary>
    /// The Google People API service.
    /// </summary>
    public class GooglePeopleService : IUserService
    {
        private static PeopleService service;

        /// <summary>
        /// Initializes a new instance of the <see cref="GooglePeopleService"/> class.
        /// </summary>
        /// <param name="peopleService">people service.</param>
        public GooglePeopleService(PeopleService peopleService)
        {
            service = peopleService;
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

        // To do: finish contact search
        public Task<List<Contact>> GetContactsAsync(string name)
        {
            return Task.FromResult(new List<Contact>());
        }

        // get people work with
        public async Task<List<MsPerson>> GetPeopleAsync(string name)
        {
            try
            {
                PeopleResource.ConnectionsResource.ListRequest peopleRequest = service.People.Connections.List("people/me");
                peopleRequest.RequestMaskIncludeField = "person.emailAddresses,person.names";

            ListConnectionsResponse connectionsResponse = await ((IClientServiceRequest<ListConnectionsResponse>)peopleRequest).ExecuteAsync();
            IList<GooglePerson> connections = connectionsResponse.Connections;

                var result = new List<MsPerson>();
                if (connections != null && connections.Count > 0)
                {
                    foreach (var people in connections)
                    {
                        // filter manually
                        var displayName = people.Names[0]?.DisplayName;
                        if (people.EmailAddresses?.Count > 0 && displayName != null && displayName.ToLower().Contains(name.ToLower()))
                        {
                            result.Add(this.GooglePersonToMsPerson(people));
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

        // search people in domain
        public Task<List<User>> GetUserAsync(string name)
        {
            return Task.FromResult(new List<User>());
        }

        private MsPerson GooglePersonToMsPerson(GooglePerson person)
        {
            var result = new MsPerson();
            if (person.Names?.Count > 0)
            {
                result.GivenName = person.Names[0]?.GivenName;
                result.Surname = person.Names[0]?.FamilyName;
                result.DisplayName = person.Names[0]?.DisplayName;
                result.UserPrincipalName = person.Names[0]?.DisplayNameLastFirst;
            }

            if (person.EmailAddresses?.Count > 0)
            {
                var addresses = new List<ScoredEmailAddress>();
                foreach (var email in person.EmailAddresses)
                {
                    addresses.Add(new ScoredEmailAddress() { Address = email.Value });
                }

                result.ScoredEmailAddresses = addresses;
            }

            return result;
        }
    }
}