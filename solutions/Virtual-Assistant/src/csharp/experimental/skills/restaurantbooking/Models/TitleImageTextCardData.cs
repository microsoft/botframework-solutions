namespace Microsoft.Ipa.Schema.Cards
{
    using System;
    using AdaptiveCards;
    using Microsoft.Bot.Solutions.Cards;

    public class TitleImageTextCardData : CardDataBase
    {
        public string Title { get; set; }

        public string ImageUrl { get; set; }

        public AdaptiveImageSize ImageSize { get; set; }

        public AdaptiveHorizontalAlignment ImageAlign { get; set; }

        public string CardText { get; set; }
    }
}