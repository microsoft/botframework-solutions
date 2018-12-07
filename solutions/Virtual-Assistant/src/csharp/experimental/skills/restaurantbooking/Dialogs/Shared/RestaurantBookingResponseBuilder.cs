using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

namespace RestaurantBooking
{
    public class RestaurantBookingResponseBuilder : BotResponseBuilder
    {
        public RestaurantBookingResponseBuilder()
           : base()
        {
            AddFormatter(new TextBotResponseFormatter());
        }
    }
}
