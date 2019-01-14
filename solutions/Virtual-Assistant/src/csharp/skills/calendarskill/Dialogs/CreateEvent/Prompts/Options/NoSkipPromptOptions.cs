using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CalendarSkill.Dialogs.CreateEvent.Prompts.Options
{
    public class NoSkipPromptOptions : PromptOptions
    {
        public NoSkipPromptOptions()
            : base()
        {
        }

        public Activity NoSkipPrompt { get; set; }
    }
}
