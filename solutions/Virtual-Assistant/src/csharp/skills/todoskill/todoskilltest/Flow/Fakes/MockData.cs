﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using ToDoSkill.Models;

namespace ToDoSkillTest.Flow.Fakes
{
    public static class MockData
    {
        public const string ToDo = "To Do";

        public const string Shopping = "Shopping";

        public const string Grocery = "Grocery";

        public const string ListType = "listType";

        public const string TaskCount = "taskCount";

        public const string ToDoTask = "toDoTask";

        public const string AfterDeleteTaskMessage = "I have deleted the item {0} for you. You have {1} items on your {2} list.";

        public const string TaskId = "MockTaskId";

        public const string LocaleEN = "en";

        public const string LocaleENUS = "en-us";

        public const string LuisGeneral = "general";

        public const string LuisToDo = "todo";

        public static List<TaskItem> MockTaskItems { get; set; } = new List<TaskItem>
        {
            new TaskItem()
            {
                Id = "ToDoNum1",
                Topic = "Play Games 1",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "ToDoNum2",
                Topic = "Play Games 2",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "ToDoNum3",
                Topic = "Play Games 3",
                IsCompleted = true
            },

            new TaskItem()
            {
                Id = "ToDoNum4",
                Topic = "Play Games 4",
                IsCompleted = true
            },

            new TaskItem()
            {
                Id = "ToDoNum5",
                Topic = "Play Games 5",
                IsCompleted = true
            },

            new TaskItem()
            {
                Id = "ToDoNum6",
                Topic = "Play Games 6",
                IsCompleted = true
            },

            new TaskItem()
            {
                Id = "ToDoNum7",
                Topic = "Play Games 7",
                IsCompleted = true
            },

            new TaskItem()
            {
                Id = "ToDoNum8",
                Topic = "Play Games 8",
                IsCompleted = true
            }
        };

        public static List<TaskItem> MockShoppingItems { get; set; } = new List<TaskItem>
        {
            new TaskItem()
            {
                Id = "ShoppingItem1",
                Topic = "Buy Shoes 1",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "ShoppingItem2",
                Topic = "Buy Shoes 2",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "ShoppingItem3",
                Topic = "Buy Shoes 3",
                IsCompleted = true
            },

            new TaskItem()
            {
                Id = "ShoppingItem4",
                Topic = "Buy Shoes 4",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "ShoppingItem5",
                Topic = "Buy Shoes 5",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "ShoppingItem6",
                Topic = "Buy Shoes 6",
                IsCompleted = true
            },
            new TaskItem()
            {
                Id = "ShoppingItem7",
                Topic = "Buy Shoes 7",
                IsCompleted = false
            }
        };

        public static List<TaskItem> MockGroceryItems { get; set; } = new List<TaskItem>
        {
            new TaskItem()
            {
                Id = "GroceryItem1",
                Topic = "Buy Milk 1",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "GroceryItem2",
                Topic = "Buy Milk 2",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "GroceryItem3",
                Topic = "Buy Milk 3",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "GroceryItem4",
                Topic = "Buy Milk 4",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "GroceryItem5",
                Topic = "Buy Milk 5",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "GroceryItem6",
                Topic = "Buy Milk 6",
                IsCompleted = true
            },

            new TaskItem()
            {
                Id = "GroceryItem7",
                Topic = "Buy Milk 7",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "GroceryItem8",
                Topic = "Buy Milk 8",
                IsCompleted = false
            },

            new TaskItem()
            {
                Id = "GroceryItem9",
                Topic = "Buy Milk 9",
                IsCompleted = false
            }
        };

        public static string MockOneNoteWebLink { get; set; } = "https://123456-my.sharepoint.com/personal/123456";
    }
}