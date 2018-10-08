using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillDialogOptions
    {
        public SkillDefinition SkillDefinition { get; set; }

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}