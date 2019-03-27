// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill.ServiceClients
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    public interface IUserService
    {
        /// <summary>
        /// Get people you are working with.
        /// </summary>
        /// <param name="name">The person's name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<List<Person>> GetPeopleAsync(string name);

        /// <summary>
        /// Get user from your organization.
        /// </summary>
        /// <param name="name">The person's name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<List<User>> GetUserAsync(string name);

        /// <summary>
        /// Get contacts from your organization.
        /// </summary>
        /// <param name="name">The contact's name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<List<Contact>> GetContactsAsync(string name);

        /// <summary>
        /// Get me.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<User> GetMeAsync();

        /// <summary>
        /// Get user photo from your organization.
        /// </summary>
        /// <param name="id">The contact's id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<string> GetUserPhotoAsync(string id);
    }
}