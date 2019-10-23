using Microsoft.Bot.Builder.Dialogs;

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
