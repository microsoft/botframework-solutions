// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Models
{
    public class CalendarItemCardData : ICardData
    {
        public string Time { get; set; }

        public string TimeColor { get; set; }

        public string Title { get; set; }

        public string Location { get; set; }

        public string Duration { get; set; }

        public bool IsSubtle { get; set; }
    }
}
