using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

namespace $safeprojectname$
{
    public class $safeprojectname$ResponseBuilder : BotResponseBuilder
    {
        public $safeprojectname$ResponseBuilder()
           : base()
        {
            AddFormatter(new TextBotResponseFormatter());
        }
    }
}
