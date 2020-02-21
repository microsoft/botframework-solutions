// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using HospitalitySkill.Responses.CheckOut;
using HospitalitySkill.Responses.Main;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Tests.Flow.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalitySkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class CheckOutFlowTests : HospitalitySkillTestBase
    {
        [TestMethod]
        public async Task CheckOutTest()
        {
            var tokens = new StringDictionary
            {
                { "Email", NonLuisUtterances.Email },
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(CheckOutUtterances.CheckOut)
                .AssertReply(AssertStartsWith(CheckOutResponses.ConfirmCheckOut))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(CheckOutResponses.EmailPrompt))
                .Send(NonLuisUtterances.Email)
                .AssertReply(AssertContains(CheckOutResponses.SendEmailMessage, tokens))
                .AssertReply(AssertContains(CheckOutResponses.CheckOutSuccess))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CheckOutAlreadyTest()
        {
            var tokens = new StringDictionary
            {
                { "Email", NonLuisUtterances.Email },
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(CheckOutUtterances.CheckOut)
                .AssertReply(AssertStartsWith(CheckOutResponses.ConfirmCheckOut))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(CheckOutResponses.EmailPrompt))
                .Send(NonLuisUtterances.Email)
                .AssertReply(AssertContains(CheckOutResponses.SendEmailMessage, tokens))
                .AssertReply(AssertContains(CheckOutResponses.CheckOutSuccess))
                .AssertReply(ActionEndMessage())
                .Send(CheckOutUtterances.CheckOut)
                .AssertReply(AssertContains(SharedResponses.HasCheckedOut))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CheckOutInvalidEmailTest()
        {
            var tokens = new StringDictionary
            {
                { "Email", NonLuisUtterances.Email },
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(CheckOutUtterances.CheckOut)
                .AssertReply(AssertStartsWith(CheckOutResponses.ConfirmCheckOut))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(CheckOutResponses.EmailPrompt))
                .Send(NonLuisUtterances.InvalidEmail)
                .AssertReply(AssertContains(CheckOutResponses.InvalidEmailPrompt))
                .Send(NonLuisUtterances.Email)
                .AssertReply(AssertContains(CheckOutResponses.SendEmailMessage, tokens))
                .AssertReply(AssertContains(CheckOutResponses.CheckOutSuccess))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }
    }
}
