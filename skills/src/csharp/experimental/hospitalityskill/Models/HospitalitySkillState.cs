// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace HospitalitySkill.Models
{
    public class HospitalitySkillState
    {
        public string Token { get; internal set; }

        public HospitalityLuis LuisResult { get; internal set; }

        public void Clear()
        {
        }
    }
}
