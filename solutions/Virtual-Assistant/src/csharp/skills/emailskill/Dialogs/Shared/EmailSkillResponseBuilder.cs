using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

namespace EmailSkill
{
    public class EmailSkillResponseBuilder : BotResponseBuilder
    {
        public EmailSkillResponseBuilder()
           : base()
        {
            AddFormatter(new TextBotResponseFormatter());
        }
    }
}
