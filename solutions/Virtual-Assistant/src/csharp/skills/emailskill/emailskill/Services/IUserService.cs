// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill.ServiceClients
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::EmailSkill.Models;

    public interface IUserService
    {
        /// <summary>
        /// Get people you are working with.
        /// </summary>
        /// <param name="name">The person's name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<List<PersonModel>> GetPeopleAsync(string name);

        /// <summary>
        /// Get user from your organization.
        /// </summary>
        /// <param name="name">The person's name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<List<PersonModel>> GetUserAsync(string name);

        /// <summary>
        /// Get contacts from your organization.
        /// </summary>
        /// <param name="name">The contact's name.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<List<PersonModel>> GetContactsAsync(string name);

        /// <summary>
        /// Get me.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<PersonModel> GetMeAsync();

        /// <summary>
        /// Get user photo from your organization.
        /// </summary>
        /// <param name="email">The user's email.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<string> GetPhotoAsync(string email);
    }
}