// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using ToDoSkill;

namespace ToDoSkillTest.Flow.Fakes
{
    public static class MockData
    {
        public static List<TaskItem> MockTaskItems = new List<TaskItem>
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

        public static List<TaskItem> MockShoppingItems = new List<TaskItem>
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
            },

            new TaskItem()
            {
                Id = "ShoppingItem8",
                Topic = "Buy Shoes 8",
                IsCompleted = false
            }
        };

        public static List<TaskItem> MockGroceryItems = new List<TaskItem>
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
            }
        };

        public static string ImageSource = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAABGdBTUEAALGPC/xhBQAAACBjSFJNAAB6JgAAgIQAAPoAAACA6AAAdTAAAOpgAAA6mAAAF3CculE8AAAACXBIWXMAAA7EAAAOxAGVKw4bAAAAAmJLR0QAAKqNIzIAAAAHdElNRQfiDAMKKhgxjYNuAAAAJXRFWHRkYXRlOmNyZWF0ZQAyMDE4LTEyLTAzVDEwOjQyOjI0KzAxOjAw3NT2SAAAACV0RVh0ZGF0ZTptb2RpZnkAMjAxOC0xMi0wM1QxMDo0MjoyNCswMTowMK2JTvQAAAAZdEVYdFNvZnR3YXJlAHd3dy5pbmtzY2FwZS5vcmeb7jwaAAAA/0lEQVQ4T6WTzQmEMBCFx0XQErypHXj0KliCPYgNiUcLsAev3uxAvVqComTzhrgu/i1hPxjykuG9SBJJSMIwFESkVfAAA6JpGoqiiEzTRKDs32MYBi3LQnVdk/TympBmTtMBHnhfSMDOumweDpCBPHliXVcax1HNdg8H/GKaJt7RcRy1svMzYJ5nsm2bte/7PH5zCijLknArAGbLsljD3HUd6yMijmM+WYA5qqqqj/Y8T3V34EHv9AVZlvGYJAmP0kx937O+4hSQ5zmlacradd1HM7g8xKIoqG1bGoZBrdzDAXieR4IgUOqazcMBeNu6bJ7/fyZcCUJkT6vgEUKINxqN2iFI/P1RAAAAAElFTkSuQmCC";
    }
}
