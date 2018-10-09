using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

namespace PointOfInterestSkill
{
    public class PointOfInterestResponseBuilder : BotResponseBuilder
    {
        public PointOfInterestResponseBuilder()
            : base()
        {
            AddFormatter(new TextBotResponseFormatter());
        }
    }
}
