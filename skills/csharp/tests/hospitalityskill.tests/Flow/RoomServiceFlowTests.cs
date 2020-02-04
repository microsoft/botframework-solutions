// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using HospitalitySkill.Responses.RoomService;
using HospitalitySkill.Tests.Flow.Strings;
using HospitalitySkill.Tests.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalitySkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class RoomServiceFlowTests : HospitalitySkillTestBase
    {
        [TestMethod]
        public async Task RoomServiceTest()
        {
            await this.GetTestFlow()
                .Send(RoomServiceUtterances.RoomService)
                .AssertReply(AssertContains(RoomServiceResponses.MenuPrompt, null, HeroCard.ContentType))
                .Send(RoomServiceUtterances.Breakfast)
                .AssertReply(AssertContains(null, null, CardStrings.MenuCard))
                .AssertReply(AssertContains(RoomServiceResponses.FoodOrder))
                .Send(RoomServiceUtterances.RoomServiceWithFood)
                .AssertReply(AssertContains(null, null, CardStrings.FoodOrderCard))
                .AssertReply(AssertContains(RoomServiceResponses.AddMore))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(RoomServiceResponses.ConfirmOrder))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RoomServiceResponses.FinalOrderConfirmation))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RoomServiceWithMenuTest()
        {
            await this.GetTestFlow()
                .Send(RoomServiceUtterances.RoomServiceWithMenu)
                .AssertReply(AssertContains(null, null, CardStrings.MenuCard))
                .AssertReply(AssertContains(RoomServiceResponses.FoodOrder))
                .Send(RoomServiceUtterances.RoomServiceWithFood)
                .AssertReply(AssertContains(null, null, CardStrings.FoodOrderCard))
                .AssertReply(AssertContains(RoomServiceResponses.AddMore))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(RoomServiceResponses.ConfirmOrder))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RoomServiceResponses.FinalOrderConfirmation))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RoomServiceWithFoodTest()
        {
            await this.GetTestFlow()
                .Send(RoomServiceUtterances.RoomServiceWithFood)
                .AssertReply(AssertContains(null, null, CardStrings.FoodOrderCard))
                .AssertReply(AssertContains(RoomServiceResponses.AddMore))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(RoomServiceResponses.ConfirmOrder))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RoomServiceResponses.FinalOrderConfirmation))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }
    }
}
