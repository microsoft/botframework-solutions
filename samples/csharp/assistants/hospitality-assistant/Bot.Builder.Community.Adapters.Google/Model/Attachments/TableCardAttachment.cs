using Microsoft.Bot.Schema;

namespace Bot.Builder.Community.Adapters.Google.Model.Attachments
{
    public class TableCardAttachment : Attachment
    {
        public TableCardAttachment()
        {
            ContentType = "google/table-card-attachment";
        }

        public TableCardAttachment(TableCard card)
        {
            ContentType = "google/table-card-attachment";
            Card = card;
        }

        public TableCard Card { get; set; }
    }
}
