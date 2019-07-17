using Microsoft.Bot.Builder.Solutions;

namespace NewsSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string BingNewsKey { get; set; }

        public string AzureMapsKey { get; set; }
    }
}