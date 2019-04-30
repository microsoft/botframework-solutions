// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace WeatherSkill.Models
{
    public class SkillState
    {
        public string Token { get; internal set; }

        public WeatherSkillLuis LuisResult { get; internal set; }

        public string Geography { get; set; }

        public void Clear()
        {
            Geography = string.Empty;
        }
    }
}
