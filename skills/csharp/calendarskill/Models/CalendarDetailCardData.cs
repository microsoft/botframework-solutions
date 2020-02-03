// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Models
{
    public class CalendarDetailCardData : ICardData
    {
        public string Content { get; set; }

        public string MeetingLink { get; set; }
    }
}
