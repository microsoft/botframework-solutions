using System;
using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Graph;
using RestaurantBooking.Models;

namespace RestaurantBooking
{
    public class RestaurantBookingState
    {
        public RestaurantBookingState()
        {
            Booking = new ReservationBooking();
            Cuisine = new List<FoodTypeInfo>();
        }

        public Luis.Reservation LuisResult { get; set; }

        public ReservationBooking Booking { get; set; }

        public List<BookingPlace> Restaurants { get; set; }

        public List<FoodTypeInfo> Cuisine { get; set; }

        public HashSet<string> AmbiguousTimexExpressions { get; set; }

        public void Clear()
        {
            LuisResult = null;
            Booking = null;
            Cuisine = null;
            AmbiguousTimexExpressions = null;
        }
    }
}
