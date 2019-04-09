// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using CalendarSkill.Models;

namespace CalendarSkill.Services
{
    public interface IUserService
    {
        Task<List<PersonModel>> GetPeopleAsync(string name);

        Task<List<PersonModel>> GetUserAsync(string name);

        /// <summary>
        /// Get user from your organization.
        /// </summary>
        /// <param name="name">The contact's name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<List<PersonModel>> GetContactsAsync(string name);

        Task<PersonModel> GetMeAsync();

        Task<string> GetPhotoAsync(string email);
    }
}
