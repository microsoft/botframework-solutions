// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace ITSMSkill.Models
{
    public class TicketCard : ICardData
    {
        public string Description { get; set; }

        public string UrgencyColor { get; set; }

        public string UrgencyLevel { get; set; }

        public string State { get; set; }

        public string OpenedTime { get; set; }

        public string Id { get; set; }

        public string ResolvedReason { get; set; }

        public string Speak { get; set; }

        public string Number { get; set; }
    }
}
