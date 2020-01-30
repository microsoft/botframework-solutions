// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace ToDoSkill.Models
{
    public class TodoItemData : ICardData
    {
        public string CheckIconUrl { get; set; }

        public string Topic { get; set; }
    }
}