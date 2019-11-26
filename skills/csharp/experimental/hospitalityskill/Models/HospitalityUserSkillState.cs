// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using HospitalitySkill.Services;

namespace HospitalitySkill.Models
{
    public class HospitalityUserSkillState
    {
        public HospitalityUserSkillState(IHotelService hotelService)
        {
            CheckedOut = false;
            LateCheckOut = false;

            // '?' for serialization
            UserReservation = hotelService?.GetReservationDetails().Result;
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
