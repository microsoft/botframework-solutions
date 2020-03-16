// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Threading.Tasks;
using ITSMSkill.Responses.Knowledge;
using ITSMSkill.Responses.Main;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Strings;
using ITSMSkill.Tests.Flow.Utterances;
using ITSMSkill.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ITSMSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class TicketUpdateFlowTests : SkillTestBase
    {
        [TestMethod]
        public async Task UpdateTest()
        {
            var attribute = new StringDictionary()
            {
                { "Attributes", string.Empty }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(TicketUpdateUtterances.Update)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTicketNumber))
                .Send(MockData.CreateTicketNumber)
                .AssertReply(AssertContains(TicketResponses.TicketTarget, null, CardStrings.Ticket))
                .AssertReply(AssertContains(TicketResponses.UpdateAttribute))
                .Send(NonLuisUtterances.Title)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertStartsWith(TicketResponses.ShowUpdates, attribute))
                .AssertReply(AssertContains(TicketResponses.UpdateAttribute))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertContains(TicketResponses.TicketUpdated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task UpdateWithNumberUrgencyTest()
        {
            var attribute = new StringDictionary()
            {
                { "Attributes", string.Empty }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(TicketUpdateUtterances.UpdateWithNumberUrgency)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(TicketResponses.TicketTarget, null, CardStrings.Ticket))
                .AssertReply(AssertStartsWith(TicketResponses.ShowUpdates, attribute))
                .AssertReply(AssertContains(TicketResponses.UpdateAttribute))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertContains(TicketResponses.TicketUpdated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }
    }
}
