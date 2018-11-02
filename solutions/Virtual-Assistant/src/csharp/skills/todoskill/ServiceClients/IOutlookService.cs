// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.ServiceClients
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// To Do bot service.
    /// </summary>
    public interface IOutlookService
    {
        /// <summary>
        /// Get To Do list.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<List<ToDoItem>> GetTasks(string listType);

        /// <summary>
        /// Add To Do to onenote page.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <param name="taskText">Task text.</param>
        /// <returns>Ture if succeed.</returns>
        Task<bool> AddTask(string listType, string taskText);

        /// <summary>
        /// Mark to do item as completed.
        /// </summary>
        /// <param name="toDoItems">To Do task items.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> MarkTasksCompleted(List<ToDoItem> toDoItems);

        /// <summary>
        /// Delete To Do.
        /// </summary>
        /// <param name="toDoItems">To Do task items.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> DeleteTasks(List<ToDoItem> toDoItems);

        /// <summary>
        /// Init To Do service.
        /// </summary>
        /// <param name="token">To Do service token.</param>
        /// <param name="taskFolderIds">Task folder ids.</param>
        /// <returns>To Do service itself.</returns>
        Task<IOutlookService> Init(string token, Dictionary<string, string> taskFolderIds);
    }
}
