using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

namespace CalendarSkill
{
    public class CalendarSkillResponseBuilder : BotResponseBuilder
    {
        public CalendarSkillResponseBuilder()
        : base()
        {
            AddFormatter(new TextBotResponseFormatter());
        }
    }
}
