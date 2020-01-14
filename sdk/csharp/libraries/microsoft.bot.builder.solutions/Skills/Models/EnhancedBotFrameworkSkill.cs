﻿using Microsoft.Bot.Builder.Skills;

namespace Microsoft.Bot.Builder.Solutions.Skills.Models
{
    // Enhanced version of BotFrameworkSkill that adds additional properties commonly needed by a Skills VA
    public class EnhancedBotFrameworkSkill : BotFrameworkSkill
    {
        // Summary:
        //     Gets or sets the Name of the skill.
        public string Name { get; set; }

        // Summary:
        //     Gets or sets the Description of the skill.
        public string Description { get; set; }
    }
}
