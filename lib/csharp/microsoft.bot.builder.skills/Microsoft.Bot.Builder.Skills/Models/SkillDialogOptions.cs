using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Skills.Models
{
    public class SkillDialogOptions
    {
        public SkillManifest SkillManifest { get; set; }

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}