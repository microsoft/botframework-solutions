// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace EventSkill.Models
{
    public class EventCardData : ICardData
    {
        public string Title { get; set; }

        public string ImageUrl { get; set; }

        public string StartDate { get; set; }

        public string Location { get; set; }

        public string Price { get; set; }

        public string Url { get; set; }
    }
}
