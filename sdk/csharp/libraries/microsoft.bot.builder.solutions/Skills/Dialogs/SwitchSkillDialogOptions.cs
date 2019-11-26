﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;
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
        /// <param name="manifest">The <see cref="SkillManifest"/> for the new skill.</param>
        public SwitchSkillDialogOptions(Activity prompt, SkillManifest manifest)
        {
            Prompt = prompt;
            Skill = manifest;
        }

        public SkillManifest Skill { get; set; }
    }
}