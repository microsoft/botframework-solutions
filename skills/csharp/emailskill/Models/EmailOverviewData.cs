// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using EmailSkill.Responses.Shared;
using Microsoft.Bot.Solutions.Responses;

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
