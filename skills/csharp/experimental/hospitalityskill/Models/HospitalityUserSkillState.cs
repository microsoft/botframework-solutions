// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace HospitalitySkill.Models
{
    public class HospitalityUserSkillState
    {
        public HospitalityUserSkillState()
        {
            CheckedOut = false;
            LateCheckOut = false;
            UserReservation = new ReservationData
            {
                CheckInDate = DateTime.Now.ToString("MMMM d, yyyy"),
                CheckOutDate = DateTime.Now.AddDays(4).ToString("MMMM d, yyyy"),
                CheckOutTimeData = new TimeSpan(12, 0, 0)
            };
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
