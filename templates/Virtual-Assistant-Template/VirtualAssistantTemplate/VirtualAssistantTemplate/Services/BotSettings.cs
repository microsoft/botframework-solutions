using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Shared;
using System.Collections.Generic;

namespace VirtualAssistantTemplate.Services
{
    public class BotSettings : BotSettingsBase
    {
        public List<SkillDefinition> Skills { get; set; } = new List<SkillDefinition>();
    }
}