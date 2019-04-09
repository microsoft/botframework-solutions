using Microsoft.Bot.Builder.Solutions.Responses;

namespace VirtualAssistant.Models
{
    public class GreetingCardModel : ICardData
    {
        public string Speak { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }
    }
}
