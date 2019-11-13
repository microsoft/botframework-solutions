// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Models;
using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Strings;
using static Luis.ITSMLuis;

namespace ITSMSkill.Tests.Flow.Utterances
{
    public class TicketUpdateUtterances : ITSMTestUtterances
    {
        public static readonly string Update = "i would like to update a ticket";

        public static readonly string UpdateWithNumberUrgency = $"update ticket {MockData.CreateTicketNumber}'s urgency to {MockData.CreateTicketUrgency}";

        public TicketUpdateUtterances()
        {
            AddIntent(Update, Intent.TicketUpdate);
            AddIntent(UpdateWithNumberUrgency, Intent.TicketUpdate, attributeType: new string[][] { new string[] { AttributeType.Urgency.ToString() } }, urgencyLevel: new string[][] { new string[] { MockData.CreateTicketUrgencyLevel.ToString() } }, ticketNumber: new string[] { MockData.CreateTicketNumber });
        }
    }
}
