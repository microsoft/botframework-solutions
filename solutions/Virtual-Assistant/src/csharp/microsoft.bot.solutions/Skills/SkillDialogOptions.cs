using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Configuration;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Skills
{
    public class SkillDialogOptions
    {
        public SkillDefinition SkillDefinition { get; set; }

        public SkillConfiguration SkillConfiguration { get; set; }
    }
}