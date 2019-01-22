// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ToDoSkill.Models;
using ToDoSkill.ServiceClients;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Fakes
{
    public class MockTaskService : ITaskService
    {
        private List<TaskItem> allToDoItems = new List<TaskItem>(MockData.MockTaskItems);

        private List<TaskItem> allShoppingItems = new List<TaskItem>(MockData.MockShoppingItems);

        private List<TaskItem> allGroceryItems = new List<TaskItem>(MockData.MockGroceryItems);

        public MockTaskService()
        {
        }

        public void ChangeData(DataOperationType.OperationType type)
        {
            if (type == DataOperationType.OperationType.KeepZeroItem)
            {
                allToDoItems.Clear();
            }
            else if (type == DataOperationType.OperationType.KeepOneItem)
            {
                allToDoItems.RemoveRange(1, allToDoItems.Count - 1);
            }
        }

        public Task<ITaskService> InitAsync(string token, Dictionary<string, string> listTypeIds, HttpClient client = null)
        {
            if (!listTypeIds.ContainsKey(MockData.ToDo))
            {
                listTypeIds.Add(MockData.ToDo, MockData.ToDo);
            }

            if (!listTypeIds.ContainsKey(MockData.Shopping))
            {
                listTypeIds.Add(MockData.Shopping, MockData.Shopping);
            }

            if (!listTypeIds.ContainsKey(MockData.Grocery))
            {
                listTypeIds.Add(MockData.Grocery, MockData.Grocery);
            }

            return Task.FromResult(this as ITaskService);
        }

        public Task<bool> DeleteTasksAsync(string listType, List<TaskItem> taskItems)
        {
            if (listType.Equals(MockData.ToDo, StringComparison.InvariantCultureIgnoreCase))
            {
                taskItems.ForEach(o => allToDoItems.Remove(allToDoItems.Find(x => x.Topic == o.Topic)));
            }
            else if (listType.Equals(MockData.Shopping, StringComparison.InvariantCultureIgnoreCase))
            {
                taskItems.ForEach(o => allShoppingItems.Remove(allShoppingItems.Find(x => x.Topic == o.Topic)));
            }
            else
            {
                taskItems.ForEach(o => allGroceryItems.Remove(allGroceryItems.Find(x => x.Topic == o.Topic)));
            }

            return Task.FromResult(true);
        }

        public Task<List<TaskItem>> GetTasksAsync(string listType)
        {
            if (listType.Equals(MockData.ToDo, StringComparison.InvariantCultureIgnoreCase))
            {
                return Task.FromResult(this.allToDoItems);
            }
            else if (listType.Equals(MockData.Shopping, StringComparison.InvariantCultureIgnoreCase))
            {
                return Task.FromResult(this.allShoppingItems);
            }
            else
            {
                return Task.FromResult(this.allGroceryItems);
            }
        }

        public Task<bool> AddTaskAsync(string listType, string taskText)
        {
            if (listType.Equals(MockData.ToDo, StringComparison.InvariantCultureIgnoreCase))
            {
                this.allToDoItems.Insert(0, new TaskItem()
                {
                    Topic = taskText,
                    IsCompleted = true,
                    Id = MockData.TaskId
                });
            }
            else if (listType.Equals(MockData.Shopping, StringComparison.InvariantCultureIgnoreCase))
            {
                this.allShoppingItems.Insert(0, new TaskItem()
                {
                    Topic = taskText,
                    IsCompleted = true,
                    Id = MockData.TaskId
                });
            }
            else
            {
                this.allGroceryItems.Insert(0, new TaskItem()
                {
                    Topic = taskText,
                    IsCompleted = true,
                    Id = MockData.TaskId
                });
            }

            return Task.FromResult(true);
        }

        public Task<bool> MarkTasksCompletedAsync(string listType, List<TaskItem> taskItems)
        {
            if (listType.Equals(MockData.ToDo, StringComparison.InvariantCultureIgnoreCase))
            {
                taskItems.ForEach(o => allToDoItems[allToDoItems.FindIndex(t => t.Id == o.Id)].IsCompleted = true);
            }
            else if (listType.Equals(MockData.Shopping, StringComparison.InvariantCultureIgnoreCase))
            {
                taskItems.ForEach(o => allShoppingItems[allShoppingItems.FindIndex(t => t.Id == o.Id)].IsCompleted = true);
            }
            else
            {
                taskItems.ForEach(o => allGroceryItems[allGroceryItems.FindIndex(t => t.Id == o.Id)].IsCompleted = true);
            }

            return Task.FromResult(true);
        }

        public Task<string> GetTaskWebLink()
        {
            return Task.FromResult(MockData.MockOneNoteWebLink);
        }
    }
}