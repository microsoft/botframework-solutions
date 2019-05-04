// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EmailSkill.Extensions;
using EmailSkill.Models;
using Microsoft.Graph;

namespace EmailSkill.Services.MSGraphAPI
{
    /// <summary>
    /// UserService.
    /// </summary>
    public class MSGraphUserService : IUserService
    {
        private IGraphServiceClient _graphClient;
        private TimeZoneInfo _timeZoneInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSGraphUserService"/> class.
        /// Init service use token.
        /// </summary>
        /// <param name="serviceClient">serviceClient.</param>
        /// <param name="timeZoneInfo">timeZoneInfo.</param>
        /// <returns>User service itself.</returns>
        public MSGraphUserService(IGraphServiceClient serviceClient, TimeZoneInfo timeZoneInfo)
        {
            this._graphClient = serviceClient;
            this._timeZoneInfo = timeZoneInfo;
        }

        /// <summary>
        /// search people by name.
        /// </summary>
        /// <param name="name">people's name.</param>
        /// <returns>List of People.</returns>
        public async Task<List<PersonModel>> GetPeopleAsync(string name)
        {
            List<Person> persons = await GetMSPeopleAsync(name);
            List<PersonModel> result = new List<PersonModel>();
            foreach (var person in persons)
            {
                if (person != null)
                {
                    result.Add(new PersonModel(person));
                }
            }

            return result;
        }

        /// <summary>
        /// GetUsersAsync.
        /// </summary>
        /// <param name="name">name.</param>
        /// <returns>Task contains List of Users.</returns>
        public async Task<List<PersonModel>> GetUserAsync(string name)
        {
            List<User> users = await GetMSUserAsync(name);
            List<PersonModel> result = new List<PersonModel>();
            foreach (User user in users)
            {
                if (user != null)
                {
                    result.Add(new PersonModel(user.ToPerson()));
                }
            }

            return result;
        }

        /// <summary>
        /// GetContactAsync.
        /// </summary>
        /// <param name="name">name.</param>
        /// <returns>Task contains List of Contacts.</returns>
        public async Task<List<PersonModel>> GetContactsAsync(string name)
        {
            List<Contact> contacts = await GetMSContactsAsync(name);
            List<PersonModel> result = new List<PersonModel>();
            foreach (Contact contact in contacts)
            {
                if (contact != null)
                {
                    result.Add(new PersonModel(contact.ToPerson()));
                }
            }

            return result;
        }

        public async Task<PersonModel> GetMeAsync()
        {
            try
            {
                var me = await _graphClient.Me.Request().GetAsync();

                if (me != null)
                {
                    var url = await GetMSUserPhotoUrlAsyc(me.Id);
                    var personMe = new PersonModel(me.ToPerson());
                    personMe.Photo = url;

                    return personMe;
                }

                return null;
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }
        }

        public async Task<string> GetPhotoAsync(string email)
        {
            var users = await this.GetUserAsync(email);

            if (users != null && users.Count > 0 && users[0].Id != null)
            {
                return await GetMSUserPhotoUrlAsyc(users[0].Id);
            }

            return null;
        }

        private static byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private async Task<string> GetMSUserPhotoUrlAsyc(string id)
        {
            var photoRequest = this._graphClient.Users[id].Photos["64x64"].Content.Request();

            Stream originalPhoto = null;
            string photoUrl = string.Empty;
            try
            {
                originalPhoto = await photoRequest.GetAsync();
                photoUrl = Convert.ToBase64String(ReadFully(originalPhoto));

                return string.Format("data:image/jpeg;base64,{0}", photoUrl);
            }
            catch (ServiceException)
            {
                return null;
            }
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