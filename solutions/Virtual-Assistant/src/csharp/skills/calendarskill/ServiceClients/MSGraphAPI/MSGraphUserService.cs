// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Extensions;
using CalendarSkill.Models;
using Microsoft.Graph;

namespace CalendarSkill.ServiceClients.MSGraphAPI
{
    /// <summary>
    /// Microsoft Graph User Service.
    /// </summary>
    public class MSGraphUserService : IUserService
    {
        private readonly IGraphServiceClient _graphClient;

        public MSGraphUserService(IGraphServiceClient graphClient)
        {
            this._graphClient = graphClient;
        }

        public async Task<List<PersonModel>> GetPeopleAsync(string name)
        {
            List<Person> persons = await GetMSPeopleAsync(name);
            List<PersonModel> result = new List<PersonModel>();
            foreach (Person person in persons)
            {
                result.Add(new PersonModel(person));
            }

            return result;
        }

        public async Task<List<PersonModel>> GetUserAsync(string name)
        {
            List<User> users = await GetMSUserAsync(name);
            List<PersonModel> result = new List<PersonModel>();
            foreach (User user in users)
            {
                result.Add(new PersonModel(user.ToPerson()));
            }

            return result;
        }

        public async Task<List<PersonModel>> GetContactsAsync(string name)
        {
            List<Contact> contacts = await GetMSContactsAsync(name);
            List<PersonModel> result = new List<PersonModel>();
            foreach (Contact contact in contacts)
            {
                result.Add(new PersonModel(contact.ToPerson()));
            }

            return result;
        }

        /// <summary>
        /// GetUsersAsync.
        /// </summary>
        /// <param name="name">name.</param>
        /// <returns>Task contains List of Users.</returns>
        private async Task<List<User>> GetMSUserAsync(string name)
        {
            var items = new List<User>();
            var optionList = new List<QueryOption>();
            var filterString = $"startswith(displayName, '{name}') or startswith(givenName,'{name}') or startswith(surname,'{name}') or startswith(mail,'{name}') or startswith(userPrincipalName,'{name}')";
            optionList.Add(new QueryOption("$filter", filterString));

            // Get the current user's profile.
            IGraphServiceUsersCollectionPage users = null;
            try
            {
                users = await _graphClient.Users.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            if (users?.Count > 0)
            {
                foreach (var user in users)
                {
                    // Filter out conference rooms.
                    var displayName = user.DisplayName ?? string.Empty;
                    if (!displayName.StartsWith("Conf Room"))
                    {
                        // Get user properties.
                        items.Add(user);
                    }

                    if (items.Count >= 10)
                    {
                        break;
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Get people whose name contains specified word.
        /// </summary>
        /// <param name="name">person name.</param>
        /// <returns>the persons list.</returns>
        private async Task<List<Person>> GetMSPeopleAsync(string name)
        {
            var items = new List<Person>();
            var optionList = new List<QueryOption>();
            var filterString = $"\"{name}\"";
            optionList.Add(new QueryOption("$search", filterString));

            // Get the current user's profile.
            IUserPeopleCollectionPage users = null;
            try
            {
                users = await _graphClient.Me.People.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            // var users = await _graphClient.Users.Request(optionList).GetAsync();
            if (users?.Count > 0)
            {
                foreach (var user in users)
                {
                    // Filter out conference rooms.
                    var displayName = user.DisplayName ?? string.Empty;
                    if (!displayName.StartsWith("Conf Room"))
                    {
                        // Get user properties.
                        items.Add(user);
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// GetContactAsync.
        /// </summary>
        /// <param name="name">name.</param>
        /// <returns>Task contains List of Contacts.</returns>
        private async Task<List<Contact>> GetMSContactsAsync(string name)
        {
            List<Contact> items = new List<Contact>();

            var optionList = new List<QueryOption>();
            var filterString = $"startswith(displayName, '{name}') or startswith(givenName,'{name}') or startswith(surname,'{name}')";
            optionList.Add(new QueryOption("$filter", filterString));

            // Get the current user's profile.
            IUserContactsCollectionPage contacts = null;
            try
            {
                contacts = await this._graphClient.Me.Contacts.Request(optionList).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            if (contacts?.Count > 0)
            {
                foreach (Contact contact in contacts)
                {
                    // Filter out conference rooms.
                    string displayName = contact.DisplayName ?? string.Empty;
                    if (!displayName.StartsWith("Conf Room"))
                    {
                        // Get user properties.
                        items.Add(contact);
                    }

                    if (items.Count >= 10)
                    {
                        break;
                    }
                }
            }

            return items;
        }
    }
}