using EmailSkill.Responses.Shared;
using Microsoft.Bot.Builder.Solutions.Responses;
using System.Collections.Generic;

namespace EmailSkill.Models
{
    public class EmailOverviewData : ICardData
    {
        public string Description { get; set; }

        public string AvatorIcon { get; set; }

        public string TotalMessageNumber { get; set; }

        public string HighPriorityMessagesNumber { get; set; }

        public string Now { get; set; }

        public string MailSourceType { get; set; }

        public string MessagesDescription { get; } = EmailCommonStrings.Messages;

        public string ImportantMessagesDescription { get; } = EmailCommonStrings.ImportantMessages;

        public string EmailIndexer { get; set; }

        public List<EmailCardData> EmailList { get; set; }
    }
}
