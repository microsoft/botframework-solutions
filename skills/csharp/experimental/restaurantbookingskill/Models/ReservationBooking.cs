﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace RestaurantBookingSkill.Models
{
    using System;

    public class ReservationBooking
    {
        public string Category { get; set; }

        public string SubCategory { get; set; }

        public DateTime? ReservationDate { get; set; }

        public DateTime? ReservationTime { get; set; }

        public int? AttendeeCount { get; set; }

        public string Location { get; set; }

        public bool Confirmed { get; set; }

        public BookingPlace BookingPlace { get; set; }

        public Meeting MeetingInfo { get; set; }
    }
}
