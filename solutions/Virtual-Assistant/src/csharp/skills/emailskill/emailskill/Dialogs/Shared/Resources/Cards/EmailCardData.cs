using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;

namespace EmailSkill.Dialogs.Shared.Resources.Cards
{
    public class EmailCardData : ICardData
    {
        public string Subject { get; set; }

        public string Sender { get; set; }

        public string NameList { get; set; }

        public string ReceivedDateTime { get; set; }

        public string EmailContent { get; set; }

        public string EmailLink { get; set; }

        public string Speak { get; set; }
    }
}