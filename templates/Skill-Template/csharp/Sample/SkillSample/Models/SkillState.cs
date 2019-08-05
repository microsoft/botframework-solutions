// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace SkillSample.Models
{
    public class SkillState
    {
        public string Token { get; set; }

        public SkillLuis LuisResult { get; set; }

        public void Clear()
        {
        }
    }
}
