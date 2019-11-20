// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs.Choices;

namespace NewsSkill.Models
{
    public class NewsSkillUserState
    {
        public NewsSkillUserState()
        {
        }

        public FoundChoice Category { get; set; }

        public string Market { get; set; }
    }
}
