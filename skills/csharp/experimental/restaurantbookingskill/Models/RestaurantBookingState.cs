// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace RestaurantBookingSkill.Models
{
    public class RestaurantBookingState
    {
        public RestaurantBookingState()
        {
            Clear();
        }

        public string Name { get; set; }

        public Luis.ReservationLuis LuisResult { get; set; }

        public ReservationBooking Booking { get; set; }

        public List<BookingPlace> Restaurants { get; set; }

        public List<FoodTypeInfo> Cuisine { get; set; }

        public Dictionary<string, string> AmbiguousTimexExpressions { get; set; }

        public void Clear()
        {
            Name = null;
            LuisResult = null;
            Booking = new ReservationBooking();
            Cuisine = new List<FoodTypeInfo>();
            AmbiguousTimexExpressions = new Dictionary<string, string>();
        }
    }
}
