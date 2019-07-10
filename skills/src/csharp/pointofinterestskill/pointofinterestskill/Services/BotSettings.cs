using Microsoft.Bot.Builder.Solutions;

namespace PointOfInterestSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string AzureMapsKey { get; set; }

        public string FoursquareClientId { get; set; }

        public string FoursquareClientSecret { get; set; }

        public string Radius { get; set; }

        public string ImageAssetLocation { get; set; }

        public string LimitSize { get; set; }
    }
}
