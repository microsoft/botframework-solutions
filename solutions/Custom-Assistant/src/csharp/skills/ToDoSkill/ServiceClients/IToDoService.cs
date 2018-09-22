// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.ServiceClients
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::ToDoSkill.Models;
    using Microsoft.Graph;

    /// <summary>
    /// To Do bot service.
    /// </summary>
    public interface IToDoService
    {
        /// <summary>
        /// Get To Do list.
        /// </summary>
        /// <returns>Tuple of To Do task activities and onenote page id.</returns>
        Task<Tuple<List<ToDoTaskActivityModel>, string>> GetMyToDoList();

        /// <summary>
        /// Get default To Do page.
        /// </summary>
        /// <returns>Default onenote page.</returns>
        Task<OnenotePage> GetDefaultToDoPage();

        /// <summary>
        /// Add To Do to onenote page.
        /// </summary>
        /// <param name="todoText">To Do text.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>Ture if succeed.</returns>
        Task<bool> AddToDoToOneNote(string todoText, string pageContentUrl);

        /// <summary>
        /// Mark to do item as completed.
        /// </summary>
        /// <param name="toDoActivity">To Do activity.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> MarkToDoItemCompleted(ToDoTaskActivityModel toDoActivity, string pageContentUrl);

        /// <summary>
        /// Mark all to do items as completed.
        /// </summary>
        /// <param name="toDoActivities">To Do activities.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> MarkAllToDoItemsCompleted(List<ToDoTaskActivityModel> toDoActivities, string pageContentUrl);

        /// <summary>
        /// Delete To Do.
        /// </summary>
        /// <param name="toDoActivity">To Do activity.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> DeleteToDo(ToDoTaskActivityModel toDoActivity, string pageContentUrl);

        /// <summary>
        /// Delete all To Dos.
        /// </summary>
        /// <param name="toDoActivities">To Do activities.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        Task<bool> DeleteAllToDos(List<ToDoTaskActivityModel> toDoActivities, string pageContentUrl);

        /// <summary>
        /// Init To Do service.
        /// </summary>
        /// <param name="token">To Do service token.</param>
        /// <param name="pageId">Onenote page id.</param>
        /// <returns>To Do service itself.</returns>
        Task<IToDoService> Init(string token, string pageId);
    }
}
