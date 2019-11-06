// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class ReservationData : ICardData
    {
        public string Title { get; set; }

        public string CheckInDate { get; set; }

        public string CheckOutDate { get; set; }

        public string CheckOutTime { get; set; }

        public ReservationData Copy()
        {
            return (ReservationData)this.MemberwiseClone();
        }
    }
}
