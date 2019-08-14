// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace AdaptiveCalendarSkill.Models
{
    public class SkillState
    {
        public string Token { get; set; }

        public AdaptiveCalendarSkillLuis LuisResult { get; set; }

        public void Clear()
        {
        }
    }
}
