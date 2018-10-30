// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkillTest.Fakes
{
    using Microsoft.Graph;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ToDoSkill;
    using ToDoSkillTest.Flow.Fakes;

    class FakeToDoService : IToDoService
    {
        private string pageId;
        private List<ToDoItem> allToDoItems = new List<ToDoItem>();

        public FakeToDoService()
        {
        }

        public async Task<IToDoService> Init(string token, string pageId)
        {
            this.pageId = pageId;
            allToDoItems = new List<ToDoItem>(FakeData.FakeToDoItems);
            return this;
        }

        public async Task<Tuple<List<ToDoItem>, string>> GetToDos()
        {
            if (string.IsNullOrEmpty(this.pageId))
            {
                this.pageId = "PageId";
            }

            return new Tuple<List<ToDoItem>, string>(allToDoItems, pageId);
        }

        public async Task<OnenotePage> GetDefaultToDoPage()
        {
            var page = new OnenotePage();
            page.Id = "PageId";
            page.ContentUrl = "https://graph.microsoft.com/v1.0/me";
            return page;
        }

        public async Task<bool> AddToDo(string todoText, string pageContentUrl)
        {
            allToDoItems.Insert(0, new ToDoItem()
            {
                Id = "AddedToDoId",
                Topic = todoText,
                IsCompleted = true
            });

            return true;
        }

        public async Task<bool> MarkAllToDosCompleted(List<ToDoItem> toDoItems, string pageContentUrl)
        {
            allToDoItems.ForEach(o => o.IsCompleted = true);
            return true;
        }

        public async Task<bool> DeleteAllToDos(List<ToDoItem> toDoItems, string pageContentUrl)
        {
            allToDoItems.Clear();
            return true;
        }

        public async Task<bool> MarkToDosCompleted(List<ToDoItem> toDoItems, string pageContentUrl)
        {
            toDoItems.ForEach(o => allToDoItems[allToDoItems.FindIndex(t => t.Id == o.Id)].IsCompleted = true);
            return true;
        }

        public async Task<bool> DeleteToDos(List<ToDoItem> toDoItems, string pageContentUrl)
        {
            toDoItems.ForEach(o => allToDoItems.RemoveAt(allToDoItems.FindIndex(t => t.Id == o.Id)));
            return true;
        }
    }
}
