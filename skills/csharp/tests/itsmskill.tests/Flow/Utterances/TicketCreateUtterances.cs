// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Strings;
using static Luis.ITSMLuis;

namespace ITSMSkill.Tests.Flow.Utterances
{
    public class TicketCreateUtterances : ITSMTestUtterances
    {
        public static readonly string Create = "create a ticket";

        public static readonly string CreateWithTitleUrgency = $"create an urgency {NonLuisUtterances.CreateTicketUrgency} ticket about {MockData.CreateTicketTitle}";

        public TicketCreateUtterances()
        {
            AddIntent(Create, Intent.TicketCreate);
            AddIntent(CreateWithTitleUrgency, Intent.TicketCreate, urgencyLevel: new string[][] { new string[] { MockData.CreateTicketUrgencyLevel.ToString() } }, ticketTitle: new string[] { MockData.CreateTicketTitle });
        }
    }
}
