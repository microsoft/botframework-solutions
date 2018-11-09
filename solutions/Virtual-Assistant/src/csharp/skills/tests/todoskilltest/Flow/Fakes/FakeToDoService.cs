// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkillTest.Fakes
{
    using Microsoft.Graph;
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using ToDoSkill;
    using ToDoSkillTest.Flow.Fakes;

    class FakeToDoService : ITaskService
    {
        private string pageId;
        private List<TaskItem> allToDoItems = new List<TaskItem>(FakeData.FakeTaskItems);

        public FakeToDoService()
        {
        
        }

        public async Task<ITaskService> InitAsync(string token, Dictionary<string, string> listTypeIds, HttpClient client = null)
        {
            if (!listTypeIds.ContainsKey("ToDo"))
                listTypeIds.Add("ToDo", "ToDo");
            return this;
        }
        //public async Task<Tuple<List<TaskItem>, string>> GetToDos()
        //{
        //    if (string.IsNullOrEmpty(this.pageId))
        //    {
        //        this.pageId = "PageId";
        //    }

        //    return new Tuple<List<TaskItem>, string>(allToDoItems, pageId);
        //}

        //public async Task<OnenotePage> GetDefaultToDoPage()
        //{
        //    var page = new OnenotePage();
        //    page.Id = "PageId";
        //    page.ContentUrl = "https://graph.microsoft.com/v1.0/me";
        //    return page;
        //}

        //public async Task<bool> AddToDo(string todoText, string pageContentUrl)
        //{
        //    allToDoItems.Insert(0, new TaskItem()
        //    {
        //        Id = "AddedToDoId",
        //        Topic = todoText,
        //        IsCompleted = true
        //    });

        //    return true;
        //}

        //public async Task<bool> MarkAllToDosCompleted(List<TaskItem> toDoItems, string pageContentUrl)
        //{
        //    allToDoItems.ForEach(o => o.IsCompleted = true);
        //    return true;
        //}

        //public async Task<bool> DeleteAllToDos(List<TaskItem> toDoItems, string pageContentUrl)
        //{
        //    allToDoItems.Clear();
        //    return true;
        //}

        //public async Task<bool> MarkToDosCompleted(List<TaskItem> toDoItems, string pageContentUrl)
        //{
        //    toDoItems.ForEach(o => allToDoItems[allToDoItems.FindIndex(t => t.Id == o.Id)].IsCompleted = true);
        //    return true;
        //}

        public async Task<bool> DeleteTasksAsync(string listType, List<TaskItem> taskItems)
        {
            taskItems.ForEach(o => allToDoItems.Remove(allToDoItems.Find(x => x.Topic == o.Topic)));
            return true;
        }
        public async Task<List<TaskItem>> GetTasksAsync(string listType)
        {
            return this.allToDoItems;
        }
        public async Task<bool> AddTaskAsync(string listType, string taskText)
        {
            this.allToDoItems.Insert(0, new TaskItem()
            {
                Topic = taskText,
                IsCompleted = true,
                Id = "AddedToDiId"
            });
            return true;
        }
        public async Task<bool> MarkTasksCompletedAsync(string listType, List<TaskItem> taskItems)
        {
            taskItems.ForEach(o => allToDoItems[allToDoItems.FindIndex(t => t.Id == o.Id)].IsCompleted = true);
            return true;
        }
    }
}
