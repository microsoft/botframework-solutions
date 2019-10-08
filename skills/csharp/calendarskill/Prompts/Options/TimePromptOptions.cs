﻿using System;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Options
{
    public class TimePromptOptions : PromptOptions
    {
        public TimePromptOptions()
            : base()
        {
        }

        public Activity NoSkipPrompt { get; set; }

        public TimeZoneInfo TimeZone { get; set; }
    }
}
