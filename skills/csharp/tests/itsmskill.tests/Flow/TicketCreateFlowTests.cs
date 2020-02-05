// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Threading.Tasks;
using ITSMSkill.Responses.Knowledge;
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
    public class TicketCreateFlowTests : SkillTestBase
    {
        [TestMethod]
        public async Task CreateTest()
        {
            var navigate = new StringDictionary
            {
                { "Navigate", string.Empty }
            };

            await this.GetTestFlow()
                .Send(TicketCreateUtterances.Create)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertContains(SharedResponses.InputDescription))
                .Send(MockData.CreateTicketDescription)
                .AssertReply(AssertStartsWith(SharedResponses.InputUrgency))
                .Send(NonLuisUtterances.CreateTicketUrgency)
                .AssertReply(AssertContains(TicketResponses.TicketCreated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CreateExistingSolveTest()
        {
            var navigate = new StringDictionary
            {
                { "Navigate", string.Empty }
            };

            await this.GetTestFlow()
                .Send(TicketCreateUtterances.Create)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Confirm)
                .AssertReply(AssertContains(SharedResponses.ActionEnded))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CreateWithTitleUrgencyTest()
        {
            var confirmTitle = new StringDictionary
            {
                { "Title", MockData.CreateTicketTitle }
            };

            var navigate = new StringDictionary
            {
                { "Navigate", string.Empty }
            };

            var confirmUrgency = new StringDictionary
            {
                { "Urgency", MockData.CreateTicketUrgencyLevel.ToLocalizedString() }
            };

            await this.GetTestFlow()
                .Send(TicketCreateUtterances.CreateWithTitleUrgency)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmTitle, confirmTitle))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertContains(SharedResponses.InputDescription))
                .Send(MockData.CreateTicketDescription)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmUrgency, confirmUrgency))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(TicketResponses.TicketCreated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CreateWithTitleUrgencyNotConfirmTest()
        {
            var confirmTitle = new StringDictionary
            {
                { "Title", MockData.CreateTicketTitle }
            };

            var navigate = new StringDictionary
            {
                { "Navigate", string.Empty }
            };

            var confirmUrgency = new StringDictionary
            {
                { "Urgency", MockData.CreateTicketUrgencyLevel.ToLocalizedString() }
            };

            await this.GetTestFlow()
                .Send(TicketCreateUtterances.CreateWithTitleUrgency)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmTitle, confirmTitle))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertContains(SharedResponses.InputDescription))
                .Send(MockData.CreateTicketDescription)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmUrgency, confirmUrgency))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(SharedResponses.InputUrgency))
                .Send(NonLuisUtterances.CreateTicketUrgency)
                .AssertReply(AssertContains(TicketResponses.TicketCreated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }
    }
}
