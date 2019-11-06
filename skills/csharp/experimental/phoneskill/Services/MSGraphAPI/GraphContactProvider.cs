// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using PhoneSkill.Common;
using PhoneSkill.Models;

namespace PhoneSkill.Services.MSGraphAPI
{
    public class GraphContactProvider : IContactProvider
    {
        private IGraphServiceClient _graphClient;

        public GraphContactProvider(IGraphServiceClient serviceClient)
        {
            this._graphClient = serviceClient;
        }

        public async Task<IList<ContactCandidate>> GetContacts()
        {
            List<ContactCandidate> candidates = await GetUsers();
            candidates.AddRange(await GetPeople());
            candidates.AddRange(await GetGraphContacts());

            return ContactDeduplicator.DeduplicateByPhoneNumbers(candidates);
        }

        private async Task<List<ContactCandidate>> GetUsers()
        {
            var optionList = new List<QueryOption>();
            var columns = "id,displayName,mobilePhone,businessPhones";
            optionList.Add(new QueryOption("$select", columns));

            IGraphServiceUsersCollectionPage users;
            try
            {
                users = await this._graphClient.Users.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            return ToContactCandidates(users);
        }

        private async Task<List<ContactCandidate>> GetPeople()
        {
            var optionList = new List<QueryOption>();
            var columns = "id,displayName,phones";
            optionList.Add(new QueryOption("$select", columns));

            IUserPeopleCollectionPage people;
            try
            {
                people = await this._graphClient.Me.People.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            return ToContactCandidates(people);
        }

        private async Task<List<ContactCandidate>> GetGraphContacts()
        {
            var optionList = new List<QueryOption>();
            var columns = "id,displayName,homePhones,mobilePhone,businessPhones";
            optionList.Add(new QueryOption("$select", columns));

            IUserContactsCollectionPage contacts;
            try
            {
                contacts = await this._graphClient.Me.Contacts.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            return ToContactCandidates(contacts);
        }

        private List<ContactCandidate> ToContactCandidates(IList<User> users)
        {
            List<ContactCandidate> candidates = new List<ContactCandidate>();
            if (users == null)
            {
                return candidates;
            }

            foreach (User user in users)
            {
                if (FilterDisplayName(user.DisplayName))
                {
                    ContactCandidate candidate = new ContactCandidate();
                    candidate.Name = user.DisplayName;

                    if (!string.IsNullOrEmpty(user.Id))
                    {
                        candidate.CorrespondingId = user.Id;
                    }

                    if (!string.IsNullOrEmpty(user.MobilePhone))
                    {
                        candidate.PhoneNumbers.Add(MakePhoneNumber(user.MobilePhone, PhoneNumberType.StandardType.MOBILE));
                    }

                    AddRange(candidate.PhoneNumbers, MakePhoneNumbers(user.BusinessPhones, PhoneNumberType.StandardType.BUSINESS));

                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        private List<ContactCandidate> ToContactCandidates(IList<Person> people)
        {
            List<ContactCandidate> candidates = new List<ContactCandidate>();
            if (people == null)
            {
                return candidates;
            }

            foreach (Person person in people)
            {
                if (FilterDisplayName(person.DisplayName))
                {
                    ContactCandidate candidate = new ContactCandidate();
                    candidate.Name = person.DisplayName;

                    if (!string.IsNullOrEmpty(person.Id))
                    {
                        candidate.CorrespondingId = person.Id;
                    }

                    if (person.Phones != null)
                    {
                        foreach (Phone phone in person.Phones)
                        {
                            if (!string.IsNullOrEmpty(phone.Number))
                            {
                                PhoneNumber phoneNumber = new PhoneNumber();
                                phoneNumber.Number = phone.Number;

                                if (phone.Type != null)
                                {
                                    switch (phone.Type)
                                    {
                                        case PhoneType.Home:
                                            phoneNumber.Type.Standardized = PhoneNumberType.StandardType.HOME;
                                            break;
                                        case PhoneType.Mobile:
                                            phoneNumber.Type.Standardized = PhoneNumberType.StandardType.MOBILE;
                                            break;
                                        case PhoneType.Business:
                                            phoneNumber.Type.Standardized = PhoneNumberType.StandardType.BUSINESS;
                                            break;
                                        default:
                                            phoneNumber.Type.FreeForm = phone.Type.ToString();
                                            break;
                                    }
                                }

                                candidate.PhoneNumbers.Add(phoneNumber);
                            }
                        }
                    }

                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        private List<ContactCandidate> ToContactCandidates(IList<Contact> contacts)
        {
            List<ContactCandidate> candidates = new List<ContactCandidate>();
            if (contacts == null)
            {
                return candidates;
            }

            foreach (Contact contact in contacts)
            {
                if (FilterDisplayName(contact.DisplayName))
                {
                    ContactCandidate candidate = new ContactCandidate();
                    candidate.Name = contact.DisplayName;

                    if (!string.IsNullOrEmpty(contact.Id))
                    {
                        candidate.CorrespondingId = contact.Id;
                    }

                    AddRange(candidate.PhoneNumbers, MakePhoneNumbers(contact.HomePhones, PhoneNumberType.StandardType.HOME));

                    if (!string.IsNullOrEmpty(contact.MobilePhone))
                    {
                        candidate.PhoneNumbers.Add(MakePhoneNumber(contact.MobilePhone, PhoneNumberType.StandardType.MOBILE));
                    }

                    AddRange(candidate.PhoneNumbers, MakePhoneNumbers(contact.BusinessPhones, PhoneNumberType.StandardType.BUSINESS));

                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        private bool FilterDisplayName(string displayName)
        {
            // Filter out conference rooms.
            return !string.IsNullOrEmpty(displayName) && !displayName.ToLowerInvariant().StartsWith("conf room");
        }

        private IList<PhoneNumber> MakePhoneNumbers(IEnumerable<string> numbers, PhoneNumberType.StandardType type)
        {
            var phoneNumbers = new List<PhoneNumber>();

            if (numbers != null)
            {
                foreach (string number in numbers)
                {
                    if (!string.IsNullOrEmpty(number))
                    {
                        phoneNumbers.Add(MakePhoneNumber(number, type));
                    }
                }
            }

            return phoneNumbers;
        }

        private PhoneNumber MakePhoneNumber(string number, PhoneNumberType.StandardType type)
        {
            PhoneNumber phoneNumber = new PhoneNumber();
            phoneNumber.Number = number;
            phoneNumber.Type.Standardized = type;
            return phoneNumber;
        }

        private void AddRange(IList<PhoneNumber> target, IList<PhoneNumber> source)
        {
            foreach (var item in source)
            {
                target.Add(item);
            }
        }
    }
}
