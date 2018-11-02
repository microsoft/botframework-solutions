﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

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
        /// Init task service.
        /// </summary>
        /// <param name="token">Task service token.</param>
        /// <param name="listTypeIds">Task list name and id dictionary.</param>
        /// <returns>To Do service itself.</returns>
        Task<ITaskService> InitAsync(string token, Dictionary<string, string> listTypeIds);
    }
}
