using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills.Models.Manifest;
using Microsoft.Bot.Builder.Solutions;

namespace $safeprojectname$.Services
{
    public class BotSettings : BotSettingsBase
    {
        public List<SkillManifest> Skills { get; set; } = new List<SkillManifest>();
    }
}