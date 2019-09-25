using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions;
using System.Collections.Generic;

namespace BingSearchSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string BingSearchKey { get; set; }

        public string BingAnswerSearchKey { get; set; }
    }
}