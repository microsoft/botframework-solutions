namespace RestaurantBooking.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Bot.Solutions.Cards;

    public class HeaderTableFooterCardData : CardDataBase
    {
        public string HeaderText { get; set; }

        // TODO: this is kind of hacky, we'll manage variable rows in the future
        public string Row1Title { get; set; }

        public string Row1Value { get; set; }

        public string Row2Title { get; set; }

        public string Row2Value { get; set; }

        public string Row3Title { get; set; }

        public string Row3Value { get; set; }

        public string Row4Title { get; set; }

        public string Row4Value { get; set; }

        public string Row5Title { get; set; }

        public string Row5Value { get; set; }

        public string FooterText { get; set; }
    }
}
