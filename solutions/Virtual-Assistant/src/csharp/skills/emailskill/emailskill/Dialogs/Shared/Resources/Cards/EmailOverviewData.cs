using Microsoft.Bot.Builder.Solutions.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmailSkill.Dialogs.Shared.Resources.Cards
{
    public class EmailOverviewData : ICardData
    {
        public string AvatorIcon { get; set; }

        public string TotalMessageNumber { get; set; }

        public string HighPriorityMessagesNumber { get; set; }

        public string Now { get; set; }

        public string MailSourceType { get; set; }
    }
}
