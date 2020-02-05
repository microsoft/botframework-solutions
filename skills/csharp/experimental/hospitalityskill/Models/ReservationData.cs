// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Models
{
    public class ReservationData : ICardData
    {
        public static readonly string DateFormat = "MMMM d, yyyy";
        public static readonly string TimeFormat = @"hh\:mm";

        public string Title { get; set; }

        public string CheckInDate { get; set; }

        public string CheckOutDate { get; set; }

        public string CheckOutTime
        {
            get { return CheckOutTimeData.ToString(TimeFormat); }
        }

        public TimeSpan CheckOutTimeData { get; set; }

        public ReservationData Copy()
        {
            return (ReservationData)this.MemberwiseClone();
        }
    }
}
