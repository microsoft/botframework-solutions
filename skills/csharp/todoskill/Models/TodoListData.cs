// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace ToDoSkill.Models
{
    public class TodoListData : ICardData
    {
        public string Title { get; set; }

        public string TotalNumber { get; set; }
    }
}