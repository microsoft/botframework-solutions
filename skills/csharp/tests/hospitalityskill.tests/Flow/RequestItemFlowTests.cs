// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using HospitalitySkill.Responses.Main;
using HospitalitySkill.Responses.RequestItem;
using HospitalitySkill.Tests.Flow.Strings;
using HospitalitySkill.Tests.Flow.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalitySkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class RequestItemFlowTests : HospitalitySkillTestBase
    {
        [TestMethod]
        public async Task RequestItemTest()
        {
            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(RequestItemUtterances.RequestItem)
                .AssertReply(AssertContains(RequestItemResponses.ItemPrompt))
                .Send(RequestItemUtterances.Item)
                .AssertReply(AssertContains(null, null, CardStrings.RequestItemCard))
                .AssertReply(AssertContains(RequestItemResponses.ItemsRequested))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RequestInvalidItemTest()
        {
            var tokens = new StringDictionary
            {
                { "Items", $"{Environment.NewLine}- {RequestItemUtterances.InvalidItem}" }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(RequestItemUtterances.RequestItem)
                .AssertReply(AssertContains(RequestItemResponses.ItemPrompt))
                .Send(RequestItemUtterances.InvalidItem)
                .AssertReply(AssertContains(RequestItemResponses.ItemNotAvailable, tokens))
                .AssertReply(AssertStartsWith(RequestItemResponses.GuestServicesPrompt))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RequestItemResponses.GuestServicesConfirm))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RequestWithItemAndInvalidItemTest()
        {
            var tokens = new StringDictionary
            {
                { "Items", $"{Environment.NewLine}- {RequestItemUtterances.InvalidItem}" }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(RequestItemUtterances.RequestWithItemAndInvalidItem)
                .AssertReply(AssertContains(RequestItemResponses.ItemNotAvailable, tokens))
                .AssertReply(AssertStartsWith(RequestItemResponses.GuestServicesPrompt))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RequestItemResponses.GuestServicesConfirm))
                .AssertReply(AssertContains(null, null, CardStrings.RequestItemCard))
                .AssertReply(AssertContains(RequestItemResponses.ItemsRequested))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }
    }
}
