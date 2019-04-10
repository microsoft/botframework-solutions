using Microsoft.Bot.Builder.Solutions.Responses;

namespace RestaurantBooking.Models
{
    public class ReservationConfirmCard : ICardData
    {
        public string Category { get; set; }

        public string Location { get; set; }

        public string ReservationDate { get; set; }

        public string ReservationDateSpeak { get; set; }

        public string ReservationTime { get; set; }

        public string AttendeeCount { get; set; }

        public string Speak { get; set; }
    }
}