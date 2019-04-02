using System.Collections.Generic;

namespace Microsoft.Bot.Builder.Skills.Models
{
    public class SkillDialogOptions
    {
        public SkillDefinition SkillDefinition { get; set; }

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}