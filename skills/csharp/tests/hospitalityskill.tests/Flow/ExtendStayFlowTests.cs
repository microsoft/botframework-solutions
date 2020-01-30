// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.ExtendStay;
using HospitalitySkill.Responses.LateCheckOut;
using HospitalitySkill.Services;
using HospitalitySkill.Tests.Flow.Strings;
using HospitalitySkill.Tests.Flow.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalitySkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ExtendStayFlowTests : HospitalitySkillTestBase
    {
        [TestMethod]
        public async Task ExtendStayTest()
        {
            var extendDate = CheckInDate + TimeSpan.FromDays(HotelService.StayDays + ExtendStayUtterances.Number);

            var tokens = new StringDictionary
            {
                { "Date", extendDate.ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(ExtendStayUtterances.ExtendStay)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendDatePrompt))
                .Send(extendDate.ToString())
                .AssertReply(AssertStartsWith(ExtendStayResponses.ConfirmExtendStay, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendStaySuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ExtendStayWithDateTest()
        {
            var extendDate = CheckInDate + TimeSpan.FromDays(HotelService.StayDays + ExtendStayUtterances.Number);

            var tokens = new StringDictionary
            {
                { "Date", extendDate.ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(ExtendStayUtterances.ExtendStayWithDate)
                .AssertReply(AssertStartsWith(ExtendStayResponses.ConfirmExtendStay, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendStaySuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ExtendStayWithNumNightsTest()
        {
            var extendDate = CheckInDate + TimeSpan.FromDays(HotelService.StayDays + ExtendStayUtterances.Number);

            var tokens = new StringDictionary
            {
                { "Date", extendDate.ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(ExtendStayUtterances.ExtendStayWithNumNights)
                .AssertReply(AssertStartsWith(ExtendStayResponses.ConfirmExtendStay, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendStaySuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ExtendStayWithTimeTest()
        {
            var tokens = new StringDictionary
            {
                { "Time", LateCheckOutUtterances.Time.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(ExtendStayUtterances.ExtendStayWithTime)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }
    }
}
