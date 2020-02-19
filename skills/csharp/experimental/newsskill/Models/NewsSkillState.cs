// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NewsSkill.Models
{
    public class NewsSkillState
    {
        public NewsSkillState()
        {
        }

        public Luis.NewsLuis LuisResult { get; set; }

        public string CurrentCoordinates { get; set; }

        public bool NewConversation { get; set; } = true;
    }
}
