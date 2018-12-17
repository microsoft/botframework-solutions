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
        }

        public RecognizerResult LuisResult { get; set; }

        public ReservationBooking Booking { get; set; }

        public List<BookingPlace> Restaurants { get; set; }

        public List<FoodTypeInfo> Cuisine { get; set; }
    }
}
