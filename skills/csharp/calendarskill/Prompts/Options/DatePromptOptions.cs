using System;
using Microsoft.Bot.Builder.Dialogs;

namespace CalendarSkill.Prompts.Options
{
    public class DatePromptOptions : CalendarPromptOptions
    {
        public DatePromptOptions(int maxReprompt = -1)
            : base(maxReprompt)
        {
        }

        public TimeZoneInfo TimeZone { get; set; }
    }
}
