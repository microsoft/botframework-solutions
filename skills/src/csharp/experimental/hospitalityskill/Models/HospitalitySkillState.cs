// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace HospitalitySkill.Models
{
    public class HospitalitySkillState
    {
        public string Token { get; set; }

        public HospitalityLuis LuisResult { get; set; }

        public ReservationData UpdatedReservation { get; set; }

        public double NumberEntity { get; set; }

        public void Clear()
        {
        }
    }
}
