using System.Collections.Generic;
using RestaurantBooking.Models;

namespace RestaurantBooking
{
    public class RestaurantBookingState
    {
        public RestaurantBookingState()
        {
            Booking = new ReservationBooking();
            Cuisine = new List<FoodTypeInfo>();
            AmbiguousTimexExpressions = new Dictionary<string, string>();
        }

        public Luis.Reservation LuisResult { get; set; }

        public ReservationBooking Booking { get; set; }

        public List<BookingPlace> Restaurants { get; set; }

        public List<FoodTypeInfo> Cuisine { get; set; }

        public Dictionary<string, string> AmbiguousTimexExpressions { get; set; }

        public void Clear()
        {
            LuisResult = null;
            Booking = null;
            Cuisine = null;
            AmbiguousTimexExpressions = null;
        }
    }
}
