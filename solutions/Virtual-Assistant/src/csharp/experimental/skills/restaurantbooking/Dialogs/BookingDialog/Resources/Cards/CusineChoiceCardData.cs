using System;
using AdaptiveCards;
using Microsoft.Bot.Solutions.Responses;

namespace RestaurantBooking.Dialogs.Resources.Cards
{
    public class CusineChoiceCardData : ICardData
    {
        public string ImageUrl { get; set; }

        public AdaptiveImageSize ImageSize { get; set; }

        public AdaptiveHorizontalAlignment ImageAlign { get; set; }

        public string Cusine { get; set; }
    }
}