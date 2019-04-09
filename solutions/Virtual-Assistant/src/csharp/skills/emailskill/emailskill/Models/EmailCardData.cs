using EmailSkill.Utilities;
using Microsoft.Bot.Builder.Solutions.Shared.Responses;

namespace EmailSkill.Models
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

        public string SenderIcon { get; set; }

        // Placeholder for important email flag or email attachment
        public string AdditionalIcon1 { get; set; } = AdaptiveCardHelper.DefaultIcon;

        // Placeholder for email attachment
        public string AdditionalIcon2 { get; set; } = AdaptiveCardHelper.DefaultIcon;

        // RecipientIcons
        public string RecipientIcon0 { get; set; } = AdaptiveCardHelper.DefaultIcon;

        public string RecipientIcon1 { get; set; } = AdaptiveCardHelper.DefaultIcon;

        public string RecipientIcon2 { get; set; } = AdaptiveCardHelper.DefaultIcon;

        public string RecipientIcon3 { get; set; } = AdaptiveCardHelper.DefaultIcon;

        public string RecipientIcon4 { get; set; } = AdaptiveCardHelper.DefaultIcon;

        public string AdditionalRecipientNumber { get; set; }
    }
}