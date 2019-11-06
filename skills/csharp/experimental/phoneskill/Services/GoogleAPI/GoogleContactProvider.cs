// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Google;
using Google.Apis.People.v1;
using Google.Apis.People.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using PhoneSkill.Common;
using PhoneSkill.Models;
using GooglePhoneNumber = Google.Apis.People.v1.Data.PhoneNumber;
using PhoneNumber = PhoneSkill.Models.PhoneNumber;

namespace PhoneSkill.Services.GoogleAPI
{
    public class GoogleContactProvider : IContactProvider
    {
        private static PeopleService service;

        public GoogleContactProvider(GoogleClient client)
        {
            service = new PeopleService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = client.GetCredential(),
                ApplicationName = client.ApplicationName,
            });
        }

        public async Task<IList<ContactCandidate>> GetContacts()
        {
            IList<Person> people = await GetPeople();
            IList<ContactCandidate> contacts = ToContactCandidates(people);
            return ContactDeduplicator.DeduplicateByPhoneNumbers(contacts);
        }

        private async Task<IList<Person>> GetPeople()
        {
            try
            {
                PeopleResource.ConnectionsResource.ListRequest peopleRequest = service.People.Connections.List("people/me");
                peopleRequest.RequestMaskIncludeField = "person.phoneNumbers,person.names";

                ListConnectionsResponse connectionsResponse = await ((IClientServiceRequest<ListConnectionsResponse>)peopleRequest).ExecuteAsync();
                if (connectionsResponse == null)
                {
                    return new List<Person>();
                }

                IList<Person> connections = connectionsResponse.Connections;
                if (connections == null)
                {
                    return new List<Person>();
                }

                return connections;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        private IList<ContactCandidate> ToContactCandidates(IList<Person> people)
        {
            List<ContactCandidate> result = new List<ContactCandidate>();
            foreach (Person person in people)
            {
                if (person.Names != null && person.Names.Count != 0 && !string.IsNullOrEmpty(person.Names[0].DisplayName))
                {
                    ContactCandidate contact = new ContactCandidate();
                    contact.Name = person.Names[0].DisplayName;

                    if (person.PhoneNumbers != null)
                    {
                        foreach (GooglePhoneNumber googlePhoneNumber in person.PhoneNumbers)
                        {
                            if (!string.IsNullOrEmpty(googlePhoneNumber.Value))
                            {
                                PhoneNumber phoneNumber = new PhoneNumber();
                                phoneNumber.Number = googlePhoneNumber.Value;

                                if (!string.IsNullOrEmpty(googlePhoneNumber.Type))
                                {
                                    phoneNumber.Type = MapPhoneNumberType(googlePhoneNumber.Type);
                                }

                                contact.PhoneNumbers.Add(phoneNumber);
                            }
                        }
                    }

                    result.Add(contact);
                }
            }

            return result;
        }

        private PhoneNumberType MapPhoneNumberType(string googlePhoneNumberType)
        {
            PhoneNumberType type = new PhoneNumberType();
            type.FreeForm = googlePhoneNumberType;

            switch (googlePhoneNumberType)
            {
                case "home":
                    type.Standardized = PhoneNumberType.StandardType.HOME;
                    break;
                case "work":
                    type.Standardized = PhoneNumberType.StandardType.BUSINESS;
                    break;
                case "mobile":
                    type.Standardized = PhoneNumberType.StandardType.MOBILE;
                    break;
            }

            return type;
        }
    }
}
