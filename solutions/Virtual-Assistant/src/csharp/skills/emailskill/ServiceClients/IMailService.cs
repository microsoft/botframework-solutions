// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill.ServiceClients
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    public interface IMailService
    {
        /// <summary>
        /// Forward email.
        /// </summary>
        /// <param name="id">The message id which need to be forward.</param>
        /// <param name="content">The additional text when forward.</param>
        /// <param name="recipients">The recipients.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task ForwardMessageAsync(string id, string content, List<Recipient> recipients);

        /// <summary>
        /// Send email.
        /// </summary>
        /// <param name="content">Email body.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="recipients">The recipients.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task SendMessageAsync(string content, string subject, List<Recipient> recipients);

        /// <summary>
        /// Reply email.
        /// </summary>
        /// <param name="id">The message id which need to be reply.</param>
        /// <param name="content">The additional text when reply.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<List<Message>> ReplyToMessageAsync(string id, string content);

        /// <summary>
        /// Get messages.
        /// </summary>
        /// <param name="startDateTime">Start date time.</param>
        /// <param name="endDateTime">End date time.</param>
        /// <param name="getUnRead">If been read.</param>
        /// <param name="isImportant">If important.</param>
        /// <param name="directlyToMe">If directly to user.</param>
        /// <param name="mailAddress">Message coming from address.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<List<Message>> GetMyMessagesAsync(DateTime startDateTime, DateTime endDateTime, bool getUnRead, bool isImportant, bool directlyToMe, string mailAddress);

        /// <summary>
        /// Delete email.
        /// </summary>
        /// <param name="id">Id of email to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteMessageAsync(string id);
    }
}