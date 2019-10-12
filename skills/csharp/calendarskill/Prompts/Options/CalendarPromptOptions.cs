using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Prompts.Options
{
    public class CalendarPromptOptions : PromptOptions
    {
        public CalendarPromptOptions(int maxReprompt = -1)
            : base()
        {
            MaxReprompt = maxReprompt;
        }

        public int MaxReprompt { get; set; }
    }
}
