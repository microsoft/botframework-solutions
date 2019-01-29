// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.ServiceClients
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using global::ToDoSkill.Dialogs.Shared.Resources;
    using global::ToDoSkill.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// To Do bot service.
    /// </summary>
    public class OutlookService : ITaskService
    {
        private const string GraphBaseUrl = "https://graph.microsoft.com/beta/me/outlook/";
        private const string OutlookTaskUrl = "https://outlook.live.com/owa/?path=/tasks";
        private HttpClient httpClient;
        private Dictionary<string, string> taskFolderIds;

        /// <summary>
        /// Initializes Outlook task service using token.
        /// </summary>
        /// <param name="token">The token used for msgraph API call.</param>
        /// <param name="taskFolderIds">Task folder ids.</param>
        /// <param name="client">The http client to call API.</param>
        /// <returns>Outlook task service itself.</returns>
        public async Task<ITaskService> InitAsync(string token, Dictionary<string, string> taskFolderIds, HttpClient client = null)
        {
            try
            {
                if (client == null)
                {
                    this.httpClient = ServiceHelper.GetHttpClient(token);
                }
                else
                {
                    this.httpClient = client;
                }

                if (!taskFolderIds.ContainsKey(ToDoStrings.ToDo))
                {
                    var taskFolderId = await GetOrCreateTaskFolderAsync(ToDoStrings.ToDo);
                    taskFolderIds.Add(ToDoStrings.ToDo, taskFolderId);
                }
                else
                {
                    // This is used for the scenario of task folder being deleted.
                    // If a task folder is deleted, will create a new one.
                    var taskFolderId = await GetOrCreateTaskFolderByIdAsync(taskFolderIds[ToDoStrings.ToDo], ToDoStrings.ToDo);
                    taskFolderIds[ToDoStrings.ToDo] = taskFolderId;
                }

                if (!taskFolderIds.ContainsKey(ToDoStrings.Grocery))
                {
                    var taskFolderId = await GetOrCreateTaskFolderAsync(ToDoStrings.Grocery);
                    taskFolderIds.Add(ToDoStrings.Grocery, taskFolderId);
                }
                else
                {
                    var taskFolderId = await GetOrCreateTaskFolderByIdAsync(taskFolderIds[ToDoStrings.Grocery], ToDoStrings.Grocery);
                    taskFolderIds[ToDoStrings.Grocery] = taskFolderId;
                }

                if (!taskFolderIds.ContainsKey(ToDoStrings.Shopping))
                {
                    var taskFolderId = await GetOrCreateTaskFolderAsync(ToDoStrings.Shopping);
                    taskFolderIds.Add(ToDoStrings.Shopping, taskFolderId);
                }
                else
                {
                    var taskFolderId = await GetOrCreateTaskFolderByIdAsync(taskFolderIds[ToDoStrings.Shopping], ToDoStrings.Shopping);
                    taskFolderIds[ToDoStrings.Shopping] = taskFolderId;
                }

                this.taskFolderIds = taskFolderIds;
                return this;
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Get To Do tasks.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <returns>List of task items.</returns>
        public async Task<List<TaskItem>> GetTasksAsync(string listType)
        {
            try
            {
                var requestUrl = GraphBaseUrl + "taskFolders/" + taskFolderIds[listType] + "/tasks";
                return await this.ExecuteTasksGetAsync(requestUrl);
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Add a task.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <param name="taskText">The task text.</param>
        /// <returns>Ture if succeed.</returns>
        public async Task<bool> AddTaskAsync(string listType, string taskText)
        {
            try
            {
                var requestUrl = GraphBaseUrl + "taskFolders/" + taskFolderIds[listType] + "/tasks";
                return await this.ExecuteTaskAddAsync(requestUrl, taskText);
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Mark tasks as completed.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <param name="taskItems">Task items.</param>
        /// <returns>True if succeed.</returns>
        public async Task<bool> MarkTasksCompletedAsync(string listType, List<TaskItem> taskItems)
        {
            try
            {
                var requestUrl = GraphBaseUrl + "tasks";
                return await this.ExecuteTasksMarkAsync(requestUrl, taskItems);
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Delete tasks.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <param name="taskItems">Task items.</param>
        /// <returns>True if succeed.</returns>
        public async Task<bool> DeleteTasksAsync(string listType, List<TaskItem> taskItems)
        {
            try
            {
                var requestUrl = GraphBaseUrl + "tasks";
                return await this.ExecuteTasksDeleteAsync(requestUrl, taskItems);
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Get task web link.
        /// </summary>
        /// <returns>Task web link.</returns>
        public async Task<string> GetTaskWebLink()
        {
            return await Task.Run(() => OutlookTaskUrl);
        }

        private async Task<string> GetOrCreateTaskFolderAsync(string taskFolderName)
        {
            var taskFolderId = await GetTaskFolderAsync(taskFolderName);
            if (string.IsNullOrEmpty(taskFolderId))
            {
                taskFolderId = await CreateTaskFolderAsync(taskFolderName);
            }

            return taskFolderId;
        }

        private async Task<string> GetOrCreateTaskFolderByIdAsync(string taskFolderId, string taskFolderName)
        {
            var newTaskFolderId = await GetTaskFolderByIdAsync(taskFolderId);
            if (string.IsNullOrEmpty(newTaskFolderId))
            {
                newTaskFolderId = await CreateTaskFolderAsync(taskFolderName);
            }

            return newTaskFolderId;
        }

        private async Task<string> GetTaskFolderAsync(string taskFolderName)
        {
            var taskFolderIdNameDic = await this.GetTaskFoldersAsync(GraphBaseUrl + "taskFolders");
            foreach (var taskFolderIdNamePair in taskFolderIdNameDic)
            {
                if (taskFolderIdNamePair.Value.Equals(taskFolderName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return taskFolderIdNamePair.Key;
                }
            }

            return null;
        }

        private async Task<string> GetTaskFolderByIdAsync(string taskFolderId)
        {
            var result = await this.httpClient.GetAsync(GraphBaseUrl + "taskFolders/" + taskFolderId);
            string newTaskFolderId = string.Empty;
            if (result.IsSuccessStatusCode)
            {
                dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
                newTaskFolderId = responseContent["id"];
            }

            return newTaskFolderId;
        }

        private async Task<string> CreateTaskFolderAsync(string taskFolderName)
        {
            var httpRequestMessage = ServiceHelper.GenerateCreateTaskFolderHttpRequest(GraphBaseUrl + "taskFolders", taskFolderName);
            var result = await this.httpClient.SendAsync(httpRequestMessage);
            dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
            if (result.IsSuccessStatusCode)
            {
                return (string)responseContent.id;
            }
            else
            {
                ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                throw serviceException;
            }
        }

        private async Task<Dictionary<string, string>> GetTaskFoldersAsync(string url)
        {
            var taskFolderIdNameDic = new Dictionary<string, string>();
            while (!string.IsNullOrEmpty(url))
            {
                var taskFoldersObject = await this.ExecuteGraphFetchAsync(url);
                foreach (var taskFolder in taskFoldersObject.value)
                {
                    string taskFolderId = taskFolder["id"];
                    string taskFolderName = taskFolder["name"];
                    taskFolderIdNameDic.Add(taskFolderId, taskFolderName);
                }

                url = taskFoldersObject["@odata.nextLink"];
            }

            return taskFolderIdNameDic;
        }

        private async Task<List<TaskItem>> ExecuteTasksGetAsync(string url)
        {
            var toDoTasks = new List<TaskItem>();
            while (!string.IsNullOrEmpty(url))
            {
                var tasksObject = await this.ExecuteGraphFetchAsync(url);
                foreach (var task in tasksObject.value)
                {
                    if (task["status"] != "completed")
                    {
                        toDoTasks.Add(new TaskItem()
                        {
                            Topic = task["subject"],
                            Id = task["id"],
                            IsCompleted = false,
                        });
                    }
                }

                url = tasksObject["@odata.nextLink"];
            }

            return toDoTasks;
        }

        private async Task<bool> ExecuteTaskAddAsync(string url, string taskText)
        {
            var httpRequestMessage = ServiceHelper.GenerateAddOutlookTaskHttpRequest(url, taskText);
            var result = await this.httpClient.SendAsync(httpRequestMessage);
            if (result.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
                ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                throw serviceException;
            }
        }

        private async Task<bool> ExecuteTasksMarkAsync(string url, List<TaskItem> taskItems)
        {
            foreach (var taskItem in taskItems)
            {
                var httpRequestMessage = ServiceHelper.GenerateMarkOutlookTaskCompletedHttpRequest(url, taskItem);
                var result = await this.httpClient.SendAsync(httpRequestMessage);
                if (!result.IsSuccessStatusCode)
                {
                    dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
                    ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                    throw serviceException;
                }
            }

            return true;
        }

        private async Task<bool> ExecuteTasksDeleteAsync(string url, List<TaskItem> taskItems)
        {
            foreach (var taskItem in taskItems)
            {
                var httpRequestMessage = ServiceHelper.GenerateDeleteOutlookTaskHttpRequest(url, taskItem);
                var result = await this.httpClient.SendAsync(httpRequestMessage);
                if (!result.IsSuccessStatusCode)
                {
                    dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
                    ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                    throw serviceException;
                }
            }

            return true;
        }

        private async Task<dynamic> ExecuteGraphFetchAsync(string url)
        {
            var result = await this.httpClient.GetAsync(url);
            dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
            if (result.IsSuccessStatusCode)
            {
                return responseContent;
            }
            else
            {
                ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                throw serviceException;
            }
        }
    }
}