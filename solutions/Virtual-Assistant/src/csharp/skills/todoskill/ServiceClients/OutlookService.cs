// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.ServiceClients
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// To Do bot service.
    /// </summary>
    public class OutlookService : ITaskService
    {
        private const string ToDoTaskFolder = "ToDo";
        private const string GroceryTaskFolder = "Grocery";
        private const string ShoppingTaskFolder = "Shopping";
        private readonly string graphBaseUrl = "https://graph.microsoft.com/beta/me/outlook/";
        private HttpClient httpClient;
        private Dictionary<string, string> taskFolderIds;

        /// <summary>
        /// Initializes Outlook task service using token.
        /// </summary>
        /// <param name="token">The token used for msgraph API call.</param>
        /// <param name="taskFolderIds">Task folder ids.</param>
        /// <returns>Outlook task service itself.</returns>
        public async Task<ITaskService> InitAsync(string token, Dictionary<string, string> taskFolderIds, HttpClient client = null)
        {
            try
            {
                this.httpClient = ServiceHelper.GetHttpClient(token);
                if (!taskFolderIds.ContainsKey(ToDoTaskFolder))
                {
                    var taskFolderId = await GetOrCreateTaskFolderAsync(ToDoTaskFolder);
                    taskFolderIds.Add(ToDoTaskFolder, taskFolderId);
                }

                if (!taskFolderIds.ContainsKey(GroceryTaskFolder))
                {
                    var taskFolderId = await GetOrCreateTaskFolderAsync(GroceryTaskFolder);
                    taskFolderIds.Add(GroceryTaskFolder, taskFolderId);
                }

                if (!taskFolderIds.ContainsKey(ShoppingTaskFolder))
                {
                    var taskFolderId = await GetOrCreateTaskFolderAsync(ShoppingTaskFolder);
                    taskFolderIds.Add(ShoppingTaskFolder, taskFolderId);
                }

                this.taskFolderIds = taskFolderIds;
                return this;
            }
            catch (Exception ex)
            {
                throw ex;
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
                var requestUrl = this.graphBaseUrl + "taskFolders/" + taskFolderIds[listType] + "/tasks";
                return await this.ExecuteTasksGetAsync(requestUrl);
            }
            catch (Exception ex)
            {
                throw ex;
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
                var requestUrl = this.graphBaseUrl + "taskFolders/" + taskFolderIds[listType] + "/tasks";
                return await this.ExecuteTaskAddAsync(requestUrl, taskText);
            }
            catch (Exception ex)
            {
                throw ex;
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
                var requestUrl = this.graphBaseUrl + "tasks";
                return await this.ExecuteTasksMarkAsync(requestUrl, taskItems);
            }
            catch (Exception ex)
            {
                throw ex;
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
                var requestUrl = this.graphBaseUrl + "tasks";
                return await this.ExecuteTasksDeleteAsync(requestUrl, taskItems);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<string> GetOrCreateTaskFolderAsync(string taskFolderName)
        {
            try
            {
                var taskFolderId = await GetTaskFolderAsync(taskFolderName);
                if (string.IsNullOrEmpty(taskFolderId))
                {
                    taskFolderId = await CreateTaskFolderAsync(taskFolderName);
                }

                return taskFolderId;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<string> GetTaskFolderAsync(string taskFolderName)
        {
            try
            {
                var taskFolderIdNameDic = await this.GetTaskFoldersAsync(this.graphBaseUrl + "taskFolders");
                foreach (var taskFolderIdNamePair in taskFolderIdNameDic)
                {
                    if (taskFolderIdNamePair.Value.Equals(taskFolderName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return taskFolderIdNamePair.Key;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<string> CreateTaskFolderAsync(string taskFolderName)
        {
            try
            {
                var httpRequestMessage = ServiceHelper.GenerateCreateTaskFolderHttpRequest(this.graphBaseUrl + "taskFolders", taskFolderName);
                var result = await this.httpClient.SendAsync(httpRequestMessage);
                dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
                return (string)responseContent.id;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<Dictionary<string, string>> GetTaskFoldersAsync(string url)
        {
            var taskFoldersObject = await this.ExecuteGraphFetchAsync(url);
            var taskFolderIdNameDic = new Dictionary<string, string>();
            foreach (var taskFolder in taskFoldersObject)
            {
                string taskFolderId = taskFolder["id"];
                string taskFolderName = taskFolder["name"];
                taskFolderIdNameDic.Add(taskFolderId, taskFolderName);
            }

            return taskFolderIdNameDic;
        }

        private async Task<List<TaskItem>> ExecuteTasksGetAsync(string url)
        {
            var tasksObject = await this.ExecuteGraphFetchAsync(url);
            var toDoTasks = new List<TaskItem>();
            foreach (var task in tasksObject)
            {
                toDoTasks.Add(new TaskItem()
                {
                    Topic = task["subject"],
                    Id = task["id"],
                    IsCompleted = task["status"] == "completed" ? true : false,
                });
            }

            return toDoTasks;
        }

        private async Task<bool> ExecuteTaskAddAsync(string url, string taskText)
        {
            var httpRequestMessage = ServiceHelper.GenerateAddOutlookTaskHttpRequest(url, taskText);
            var result = await this.httpClient.SendAsync(httpRequestMessage);
            return result.IsSuccessStatusCode;
        }

        private async Task<bool> ExecuteTasksMarkAsync(string url, List<TaskItem> taskItems)
        {
            foreach (var taskItem in taskItems)
            {
                var httpRequestMessage = ServiceHelper.GenerateMarkOutlookTaskCompletedHttpRequest(url, taskItem);
                var result = await this.httpClient.SendAsync(httpRequestMessage);
                if (!result.IsSuccessStatusCode)
                {
                    throw new Exception();
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
                    throw new Exception();
                }
            }

            return true;
        }

        private async Task<dynamic> ExecuteGraphFetchAsync(string url)
        {
            var result = await this.httpClient.GetStringAsync(url);
            dynamic content = JObject.Parse(result);
            return content.value;
        }
    }
}
