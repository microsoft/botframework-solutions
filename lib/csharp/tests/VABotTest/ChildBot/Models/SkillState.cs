// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace ChildBot.Models
{
    public class SkillState
    {
        public string Token { get; set; }

        public SkillBotLuis BotLuisResult { get; set; }

        public void Clear()
        {
        }
    }
}
