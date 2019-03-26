using Microsoft.Bot.Builder.Solutions.Responses;

namespace VirtualAssistant.Models
{
    public class GreetingCardModel : ICardData
    {
        public string HeaderImageUrl { get; set; }

        public string BackgroundImageUrl { get; set; }

        public string ColumnBackgroundImageUrl { get; set; }

        public string Speak { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }
    }
}
