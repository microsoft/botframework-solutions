namespace RestaurantBooking
{
    using Microsoft.Bot.Solutions.Dialogs;
    using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;

    public class RestaurantBookingResponseBuilder : BotResponseBuilder
    {
        public RestaurantBookingResponseBuilder()
           : base()
        {
            this.AddFormatter(new TextBotResponseFormatter());
        }
    }
}
