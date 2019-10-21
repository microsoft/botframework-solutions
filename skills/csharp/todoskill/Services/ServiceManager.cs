// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.Services
{
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using ToDoSkill.Models;

    public class ServiceManager : IServiceManager
    {
        public ServiceManager()
        {
        }

        public ServiceManager(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Init task service.
        /// </summary>
        /// <param name="token">Task service token.</param>
        /// <param name="listTypeIds">Task list name and id dictionary.</param>
        /// <param name="taskServiceType">The task service type.</param>
        /// <returns>Task service itself.</returns>
        public ITaskService InitTaskService(string token, Dictionary<string, string> listTypeIds, ServiceProviderType taskServiceType)
        {
            ITaskService taskService;
            if (taskServiceType == ServiceProviderType.OneNote)
            {
                var oneNoteService = new OneNoteService(Configuration);
                taskService = oneNoteService.InitAsync(token, listTypeIds).Result;
            }
            else
            {
                var outlookService = new OutlookService(Configuration);
                taskService = outlookService.InitAsync(token, listTypeIds).Result;
            }

            return taskService;
        }

        /// <summary>
        /// Init mail service.
        /// </summary>
        /// <param name="token">Task service token.</param>
        /// <returns>Mail service itself.</returns>
        public IMailService InitMailService(string token)
        {
            var service = new MailService();
            var mailService = service.InitAsync(token).Result;
            return mailService;
        }
    }
}