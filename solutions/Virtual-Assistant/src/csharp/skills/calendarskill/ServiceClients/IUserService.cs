// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace CalendarSkill
{
    public interface IUserService
    {
        Task<List<Person>> GetPeopleAsync(string name);

        Task<List<User>> GetUserAsync(string name);

        /// <summary>
        /// Get user from your organization.
        /// </summary>
        /// <param name="name">The contact's name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<List<Contact>> GetContactsAsync(string name);
    }
}
