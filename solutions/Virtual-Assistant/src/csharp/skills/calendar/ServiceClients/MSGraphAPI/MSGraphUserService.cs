﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace CalendarSkill
{
    /// <summary>
    /// Microsoft Graph User Service.
    /// </summary>
    public class MSGraphUserService : IUserService
    {
        private GraphServiceClient graphClient;

        private TimeZoneInfo timeZoneInfo;

        public MSGraphUserService(string token, TimeZoneInfo info)
        {
            graphClient = GetAuthenticatedClient(token, info);
            timeZoneInfo = info;
        }

        public static GraphServiceClient GetAuthenticatedClient(string accessToken, TimeZoneInfo info)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + info.Id + "\"");
                        await Task.CompletedTask;
                    }));
            return graphClient;
        }

        /// <summary>
        /// GetUsersAsync.
        /// </summary>
        /// <param name="name">name.</param>
        /// <returns>Task contains List of Users.</returns>
        public async Task<List<User>> GetUserAsync(string name)
        {
            var items = new List<User>();
            var optionList = new List<QueryOption>();
            var filterString = $"startswith(displayName, '{name}') or startswith(givenName,'{name}') or startswith(surname,'{name}') or startswith(mail,'{name}') or startswith(userPrincipalName,'{name}')";
            optionList.Add(new QueryOption("$filter", filterString));

            // Get the current user's profile.
            var users = await graphClient.Users.Request(optionList).GetAsync();

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
        public async Task<List<Person>> GetPeopleAsync(string name)
        {
            var items = new List<Person>();
            var optionList = new List<QueryOption>();
            var filterString = $"\"{name}\"";
            optionList.Add(new QueryOption("$search", filterString));

            // Get the current user's profile.
            var users = await graphClient.Me.People.Request(optionList).GetAsync();

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
        /// Get the current user's profile.
        /// </summary>
        /// <returns>the current user's profile.</returns>
        public async Task<List<User>> GetMe()
        {
            var items = new List<User>();

            // Get the current user's profile.
            var me = await graphClient.Me.Request().GetAsync();

            if (me != null)
            {
                // Get user properties.
                items.Add(me);
            }

            return items;
        }
    }
}