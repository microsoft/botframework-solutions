// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace EmailSkill
{
    /// <summary>
    /// UserService.
    /// </summary>
    public class UserService : IUserService
    {
        private IGraphServiceClient _graphClient;
        private TimeZoneInfo _timeZoneInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// Init service use token.
        /// </summary>
        /// <param name="serviceClient">serviceClient.</param>
        /// <param name="timeZoneInfo">timeZoneInfo.</param>
        /// <returns>User service itself.</returns>
        public UserService(IGraphServiceClient serviceClient, TimeZoneInfo timeZoneInfo)
        {
            this._graphClient = serviceClient;
            this._timeZoneInfo = timeZoneInfo;
        }

        /// <summary>
        /// GetUsersAsync.
        /// </summary>
        /// <param name="name">name.</param>
        /// <returns>Task contains List of Users.</returns>
        public async Task<List<User>> GetUserAsync(string name)
        {
            List<User> items = new List<User>();

            var optionList = new List<QueryOption>();
            var filterString = $"startswith(displayName, '{name}') or startswith(givenName,'{name}') or startswith(surname,'{name}') or startswith(mail,'{name}') or startswith(userPrincipalName,'{name}')";
            optionList.Add(new QueryOption("$filter", filterString));

            // Get the current user's profile.
            var users = await this._graphClient.Users.Request(optionList).GetAsync();

            if (users?.Count > 0)
            {
                foreach (User user in users)
                {
                    // Filter out conference rooms.
                    string displayName = user.DisplayName ?? string.Empty;
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
        /// search people by name.
        /// </summary>
        /// <param name="name">people's name.</param>
        /// <returns>List of People.</returns>
        public async Task<List<Person>> GetPeopleAsync(string name)
        {
            List<Person> items = new List<Person>();
            var optionList = new List<QueryOption>();
            var filterString = $"\"{name}\"";
            optionList.Add(new QueryOption("$search", filterString));

            // Get the current user's profile.
            var users = await this._graphClient.Me.People.Request(optionList).GetAsync();

            // var users = await _graphClient.Users.Request(optionList).GetAsync();
            if (users?.Count > 0)
            {
                foreach (Person user in users)
                {
                    // Filter out conference rooms.
                    string displayName = user.DisplayName ?? string.Empty;
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
        /// Get the current user's profile.
        /// </summary>
        /// <returns>List of User.</returns>
        public async Task<List<User>> GetMe()
        {
            List<User> items = new List<User>();

            // Get the current user's profile.
            User me = await this._graphClient.Me.Request().GetAsync();

            if (me != null)
            {
                // Get user properties.
                items.Add(me);
            }

            return items;
        }

        /// <summary>
        /// Get the current user's manager.
        /// </summary>
        /// <returns>Manager.</returns>
        public async Task<User> GetMyManager()
        {
            // Get the current user's manager.
            if (await this._graphClient.Me.Manager.Request().GetAsync() is User manager)
            {
                return manager;
            }

            return null;
        }

        /// <summary>
        /// Get a specified user.
        /// </summary>
        /// <param name="id">user id.</param>
        /// <returns>List of User.</returns>
        public async Task<List<User>> GetUser(string id)
        {
            List<User> items = new List<User>();

            // Get the user.
            User user = await this._graphClient.Users[id].Request().GetAsync();

            if (user != null)
            {
                // Get user properties.
                items.Add(user);
            }

            return items;
        }
    }
}