// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.ServiceClients
{
    using System.Threading.Tasks;

    /// <summary>
    /// Mail service used to call real apis.
    /// </summary>
    public interface IMailService
    {
        /// <summary>
        /// Send an email message.
        /// </summary>
        /// <param name="content">Email Body.</param>
        /// <param name="subject">Eamil Subject.</param>
        /// <returns>Completed Task.</returns>
        Task SendMessageAsync(string content, string subject);

        /// <summary>
        /// Get the sender address of current user.
        /// </summary>
        /// <returns>The sender address.</returns>
        Task<string> GetSenderMailAddressAsync();

        /// <summary>
        /// Init mail service.
        /// </summary>
        /// <param name="token">Mail service token.</param>
        /// <returns>Mail service itself.</returns>
        Task<IMailService> InitAsync(string token);
    }
}