// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.Solutions.Skills.Models;
using Microsoft.Bot.Builder.Solutions.Skills.Models.Manifest;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Skills.Dialogs
{
    public class SwitchSkillDialogOptions : PromptOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SwitchSkillDialogOptions"/> class.
        /// </summary>
        /// <param name="prompt">The <see cref="Activity"/> to display when prompting to switch skills.</param>
        /// <param name="skill">The <see cref="EnhancedBotFrameworkSkill"/> for the new skill.</param>
        public SwitchSkillDialogOptions(Activity prompt, EnhancedBotFrameworkSkill skill)
        {
            Prompt = prompt;
            Skill = skill;
        }

        public EnhancedBotFrameworkSkill Skill { get; set; }
    }
}