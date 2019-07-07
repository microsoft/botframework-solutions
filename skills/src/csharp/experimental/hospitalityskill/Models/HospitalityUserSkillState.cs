// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace HospitalitySkill.Models
{
    public class HospitalityUserSkillState
    {
        public HospitalityUserSkillState()
        {
            CheckedOut = false;
        }

        public bool CheckedOut { get; set; }

        public void Clear()
        {
        }
    }
}
