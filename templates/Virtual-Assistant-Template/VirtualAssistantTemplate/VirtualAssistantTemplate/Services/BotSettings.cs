using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions;
using System.Collections.Generic;

namespace VirtualAssistantTemplate.Services
{
    public class BotSettings : BotSettingsBase
    {
        public List<SkillManifest> Skills { get; set; } = new List<SkillManifest>();
    }
}