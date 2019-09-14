using System.Collections.Generic;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;

namespace Microsoft.Bot.Builder.Solutions.Skills.Models
{
    public class SkillDialogOptions
    {
        public SkillManifest SkillManifest { get; set; }

        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
    }
}
