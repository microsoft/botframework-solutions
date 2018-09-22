// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Bot.Solutions.Cards;

namespace CalendarSkill
{
    public class CalendarCardData : CardDataBase
    {
        public string Title { get; set; }

        public string Content { get; set; }

        public string MeetingLink { get; set; }

        public string Speak { get; set; }
    }
}
