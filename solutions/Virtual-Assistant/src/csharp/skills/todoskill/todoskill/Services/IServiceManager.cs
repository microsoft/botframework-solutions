// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.ServiceClients
{
    using System.Collections.Generic;
    using global::ToDoSkill.Dialogs.Shared;

    public interface IServiceManager
    {
        /// <summary>
        /// Init task service.
        /// </summary>
        /// <param name="token">Task service token.</param>
        /// <param name="listTypeIds">Task list name and id dictionary.</param>
        /// <param name="taskServiceType">The task service type.</param>
        /// <returns>Task service itself.</returns>
        ITaskService InitTaskService(string token, Dictionary<string, string> listTypeIds, ServiceProviderTypes.ProviderTypes taskServiceType);

        /// <summary>
        /// Init mail service.
        /// </summary>
        /// <param name="token">Task service token.</param>
        /// <returns>Mail service itself.</returns>
        IMailService InitMailService(string token);
    }
}