// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkillTest.Flow.Fakes
{
    using System.Collections.Generic;
    using ToDoSkill;

    public static class FakeData
    {
        public static List<ToDoItem> FakeToDoItems = new List<ToDoItem>
        {
            new ToDoItem()
            {
                Id = "ToDoNum1",
                Topic = "Play Games 1",
                IsCompleted = false
            },

            new ToDoItem()
            {
                Id = "ToDoNum2",
                Topic = "Play Games 2",
                IsCompleted = false
            },

            new ToDoItem()
            {
                Id = "ToDoNum3",
                Topic = "Play Games 3",
                IsCompleted = true
            },

            new ToDoItem()
            {
                Id = "ToDoNum4",
                Topic = "Play Games 4",
                IsCompleted = true
            },

            new ToDoItem()
            {
                Id = "ToDoNum5",
                Topic = "Play Games 5",
                IsCompleted = true
            },

            new ToDoItem()
            {
                Id = "ToDoNum6",
                Topic = "Play Games 6",
                IsCompleted = true
            },

            new ToDoItem()
            {
                Id = "ToDoNum7",
                Topic = "Play Games 7",
                IsCompleted = true
            },

            new ToDoItem()
            {
                Id = "ToDoNum8",
                Topic = "Play Games 8",
                IsCompleted = true
            }
        };
    }
}
