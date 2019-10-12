using System;
using CalendarSkill.Prompts.Options;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Options
{
    public class TimePromptOptions : CalendarPromptOptions
    {
        public TimePromptOptions(int maxReprompt = -1)
            : base(maxReprompt)
        {
        }

        public Activity NoSkipPrompt { get; set; }

        public TimeZoneInfo TimeZone { get; set; }
    }
}
