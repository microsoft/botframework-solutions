// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.ServiceClients
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using global::ToDoSkill.Models;

    /// <summary>
    /// Task service.
    /// </summary>
    public interface ITaskService
    {
        /// <summary>
        /// Get tasks.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <returns>List of tasks.</returns>
        Task<List<TaskItem>> GetTasksAsync(string listType);

        /// <summary>
        /// Add a task to list.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <param name="taskText">Task text.</param>
        /// <returns>Ture if succeed.</returns>
        Task<bool> AddTaskAsync(string listType, string taskText);

        /// <summary>
        /// Mark tasks as completed.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <param name="taskItems">Task items.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> MarkTasksCompletedAsync(string listType, List<TaskItem> taskItems);

        /// <summary>
        /// Delete tasks.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <param name="taskItems">Task items.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> DeleteTasksAsync(string listType, List<TaskItem> taskItems);

        /// <summary>
        /// Get task web link.
        /// </summary>
        /// <returns>Task web link.</returns>
        Task<string> GetTaskWebLink();

        /// <summary>
        /// Init task service.
        /// </summary>
        /// <param name="token">Task service token.</param>
        /// <param name="listTypeIds">Task list name and id dictionary.</param>
        /// <param name="client">the httpclient for making the API request.</param>
        /// <returns>Task service itself.</returns>
        Task<ITaskService> InitAsync(string token, Dictionary<string, string> listTypeIds, HttpClient client = null);
    }
}