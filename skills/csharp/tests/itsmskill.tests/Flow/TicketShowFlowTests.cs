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
    public class TicketShowFlowTests : SkillTestBase
    {
        [TestMethod]
        public async Task ShowTest()
        {
            var navigate = new StringDictionary
            {
                { "Navigate", string.Empty }
            };

            var attribute = new StringDictionary
            {
                { "Attributes", string.Empty }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(TicketShowUtterances.Show)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Confirm)
                .AssertReply(AssertStartsWith(TicketResponses.ShowAttribute))
                .Send(NonLuisUtterances.Text)
                .AssertReply(AssertContains(SharedResponses.InputSearch))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertStartsWith(TicketResponses.ShowAttribute))
                .Send(NonLuisUtterances.Urgency)
                .AssertReply(AssertStartsWith(SharedResponses.InputUrgency))
                .Send(NonLuisUtterances.CreateTicketUrgency)
                .AssertReply(AssertStartsWith(TicketResponses.ShowAttribute))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(TicketResponses.ShowConstraints, attribute))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertContains(SharedResponses.ActionEnded))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ShowWithTitleTest()
        {
            var navigate = new StringDictionary
            {
                { "Navigate", string.Empty }
            };

            var attribute = new StringDictionary
            {
                { "Attributes", string.Empty }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(TicketShowUtterances.ShowWithTitle)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertStartsWith(TicketResponses.ShowConstraints, attribute))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertContains(SharedResponses.ActionEnded))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }
    }
}
