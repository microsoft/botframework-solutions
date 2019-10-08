// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.Services
{
    using System.Collections.Generic;
    using ToDoSkill.Models;

    public interface IServiceManager
    {
        /// <summary>
        /// Init task service.
        /// </summary>
        /// <param name="token">Task service token.</param>
        /// <param name="listTypeIds">Task list name and id dictionary.</param>
        /// <param name="taskServiceType">The task service type.</param>
        /// <returns>Task service itself.</returns>
        ITaskService InitTaskService(string token, Dictionary<string, string> listTypeIds, ServiceProviderType taskServiceType);

        /// <summary>
        /// Init mail service.
        /// </summary>
        /// <param name="token">Task service token.</param>
        /// <returns>Mail service itself.</returns>
        IMailService InitMailService(string token);
    }
}