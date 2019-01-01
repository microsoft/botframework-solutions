using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

namespace PointOfInterestSkill.Dialogs.Shared
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