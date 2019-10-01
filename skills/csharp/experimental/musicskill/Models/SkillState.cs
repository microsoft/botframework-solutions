// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace MusicSkill.Models
{
    public class SkillState
    {
        public string Token { get; set; }

        public MusicSkillLuis LuisResult { get; set; }

        public void Clear()
        {
        }
    }
}
