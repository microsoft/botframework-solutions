// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ToDoSkill;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Fakes
{
    public class FakeToDoService : ITaskService
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
