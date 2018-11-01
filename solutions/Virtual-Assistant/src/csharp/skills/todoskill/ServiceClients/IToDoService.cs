// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// To Do bot service.
    /// </summary>
    public interface IToDoService
    {
        /// <summary>
        /// Get To Do list.
        /// </summary>
        /// <returns>Tuple of to dos and onenote page id.</returns>
        Task<Tuple<List<ToDoItem>, string>> GetToDos(string listType);

        /// <summary>
        /// Get default To Do page.
        /// </summary>
        /// <returns>Default onenote page.</returns>
        Task<OnenotePage> GetDefaultToDoPage(string listType);

        /// <summary>
        /// Add to do to onenote page.
        /// </summary>
        /// <param name="todoText">To Do text.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>Ture if succeed.</returns>
        Task<bool> AddToDo(string todoText, string pageContentUrl);

        /// <summary>
        /// Mark to dos as completed.
        /// </summary>
        /// <param name="toDoActivities">To Do activities.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> MarkToDosCompleted(List<ToDoItem> toDoActivities, string pageContentUrl);

        /// <summary>
        /// Mark all to dos as completed.
        /// </summary>
        /// <param name="toDoActivities">To Do activities.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> MarkAllToDosCompleted(List<ToDoItem> toDoActivities, string pageContentUrl);

        /// <summary>
        /// Delete to dos.
        /// </summary>
        /// <param name="toDoActivities">To Do activities.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> DeleteToDos(List<ToDoItem> toDoActivities, string pageContentUrl);

        /// <summary>
        /// Delete all to dos.
        /// </summary>
        /// <param name="toDoActivities">To Do activities.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> DeleteAllToDos(List<ToDoItem> toDoActivities, string pageContentUrl);

        /// <summary>
        /// Init To Do service.
        /// </summary>
        /// <param name="token">To Do service token.</param>
        /// <param name="pageId">Onenote page id.</param>
        /// <returns>To Do service itself.</returns>
        Task<IToDoService> Init(string token, Dictionary<string, string> pageIds);
    }
}
