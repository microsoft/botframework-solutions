using System;
using AdaptiveCards;
using Microsoft.Bot.Solutions.Responses;

namespace RestaurantBooking.Shared.Resources.Cards
{
    public class ReservationConfirmationData : ICardData
    {
        public string Title { get; set; }

        public string ImageUrl { get; set; }

        public AdaptiveImageSize ImageSize { get; set; }

        public AdaptiveHorizontalAlignment ImageAlign { get; set; }

        public string BookingPlace { get; set; }

        public string Location { get; set; }

        public string ReservationDate { get; set; }

        public string ReservationDateSpeak { get; set; }

        public string ReservationTime { get; set; }

        public string AttendeeCount { get; set; }

        public string Speak { get; set; }
    }
}
