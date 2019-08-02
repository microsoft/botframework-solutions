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
            LateCheckOut = false;
        }

        public bool CheckedOut { get; set; }

        public bool LateCheckOut { get; set; }

        public ReservationData UserReservation { get; set; }

        public string Email { get; set; }

        public void Clear()
        {
        }
    }
}
