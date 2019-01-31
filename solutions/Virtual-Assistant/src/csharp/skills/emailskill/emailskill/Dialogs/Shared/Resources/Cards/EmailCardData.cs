using AdaptiveCards;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Cards;
using Microsoft.Bot.Solutions.Resources;

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

        public Attachment BuildCardAttachment(string json)
        {
            // replace variables in json with values above
            // use regex?
            var card = AdaptiveCard.FromJson(json).Card;
            return new Attachment(AdaptiveCard.ContentType, content: card);
        }
    }
}