namespace RestaurantBooking.Models
{
    using System;

    public abstract class Booking
    {
        public string Category { get; set; }

        public string SubCategory { get; set; }

        public DateTime? Date { get; set; }

        public DateTime? Time { get; set; }

        public string AttendeeCount { get; set; }

        public BookingPlace BookingPlace { get; set; }

        public string Location { get; set; }

        public bool Confirmed { get; set; }
    }
}
