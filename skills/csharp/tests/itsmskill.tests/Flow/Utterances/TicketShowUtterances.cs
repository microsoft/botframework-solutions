// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Strings;
using static Luis.ITSMLuis;

namespace ITSMSkill.Tests.Flow.Utterances
{
    public class TicketShowUtterances : ITSMTestUtterances
    {
        public static readonly string Show = "show my tickets";

        public static readonly string ShowWithTitle = $"show my tickets about {MockData.CreateTicketTitle}";

        public TicketShowUtterances()
        {
            AddIntent(Show, Intent.TicketShow);
            AddIntent(ShowWithTitle, Intent.TicketShow, ticketTitle: new string[] { MockData.CreateTicketTitle });
        }
    }
}
