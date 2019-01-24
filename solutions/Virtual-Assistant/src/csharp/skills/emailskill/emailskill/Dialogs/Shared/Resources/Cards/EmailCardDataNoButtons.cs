using Microsoft.Bot.Solutions.Cards;

namespace EmailSkill.Dialogs.Shared.Resources.Cards
{
    public class EmailCardDataNoButtons : CardDataBase
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