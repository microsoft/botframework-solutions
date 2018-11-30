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
        private List<TaskItem> allToDoItems = new List<TaskItem>(FakeData.FakeTaskItems);

        public FakeToDoService()
        {
        }

        public Task<ITaskService> InitAsync(string token, Dictionary<string, string> listTypeIds, HttpClient client = null)
        {
            if (!listTypeIds.ContainsKey("ToDo"))
            {
                listTypeIds.Add("ToDo", "ToDo");
            }

            return Task.FromResult(this as ITaskService);
        }

        public Task<bool> DeleteTasksAsync(string listType, List<TaskItem> taskItems)
        {
            taskItems.ForEach(o => allToDoItems.Remove(allToDoItems.Find(x => x.Topic == o.Topic)));
            return Task.FromResult(true);
        }

        public Task<List<TaskItem>> GetTasksAsync(string listType)
        {
            return Task.FromResult(this.allToDoItems);
        }

        public Task<bool> AddTaskAsync(string listType, string taskText)
        {
            this.allToDoItems.Insert(0, new TaskItem()
            {
                Topic = taskText,
                IsCompleted = true,
                Id = "AddedToDiId"
            });
            return Task.FromResult(true);
        }

        public Task<bool> MarkTasksCompletedAsync(string listType, List<TaskItem> taskItems)
        {
            taskItems.ForEach(o => allToDoItems[allToDoItems.FindIndex(t => t.Id == o.Id)].IsCompleted = true);
            return Task.FromResult(true);
        }
    }
}
