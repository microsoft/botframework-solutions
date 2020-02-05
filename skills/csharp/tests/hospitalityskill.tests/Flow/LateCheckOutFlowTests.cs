// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.LateCheckOut;
using HospitalitySkill.Services;
using HospitalitySkill.Tests.Flow.Strings;
using HospitalitySkill.Tests.Flow.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalitySkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class LateCheckOutFlowTests : HospitalitySkillTestBase
    {
        [TestMethod]
        public async Task LateCheckOutTest()
        {
            var tokens = new StringDictionary
            {
                { "Time", HotelService.LateTime.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(LateCheckOutUtterances.LateCheckOut)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task LateCheckOutWithExceededTimeTest()
        {
            var tokens = new StringDictionary
            {
                { "Time", HotelService.LateTime.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(LateCheckOutUtterances.LateCheckOutWithExceededTime)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }
    }
}
