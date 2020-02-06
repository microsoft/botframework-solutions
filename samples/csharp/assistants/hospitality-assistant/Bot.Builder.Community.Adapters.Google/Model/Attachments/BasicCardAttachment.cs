using Microsoft.Bot.Schema;

namespace Bot.Builder.Community.Adapters.Google.Model.Attachments
{
    public class BasicCardAttachment : Attachment
    {
        public BasicCardAttachment()
        {
            ContentType = "google/card-attachment";
        }

        public BasicCardAttachment(BasicCard card)
        {
            ContentType = "google/card-attachment";
            Card = card;
        }

        public BasicCard Card { get; set; }
    }
}
