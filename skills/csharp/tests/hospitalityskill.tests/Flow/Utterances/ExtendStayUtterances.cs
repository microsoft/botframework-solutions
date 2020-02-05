// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using HospitalitySkill.Models;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder.AI.Luis;
using static Luis.HospitalityLuis;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class ExtendStayUtterances : HospitalityTestUtterances
    {
        public static readonly string HotelNights = "days";

        public static readonly int Number = 2;

        public static readonly DateTime Date = HospitalitySkillTestBase.CheckInDate.AddDays(HotelService.StayDays + Number);

        public static readonly string ExtendStay = "can i stay longer";

        public static readonly string ExtendStayWithNumNights = $"check out {Number} {HotelNights} later";

        public static readonly string ExtendStayWithDate = $"i want to stay until {Date.ToString()}";

        public static readonly string ExtendStayWithTime = $"i want to stay until {LateCheckOutUtterances.Time.ToString()}";

        public ExtendStayUtterances()
        {
            AddIntent(ExtendStay, Intent.ExtendStay);
            AddIntent(ExtendStayWithNumNights, Intent.ExtendStay, numNights: new NumNightsClass[] { new NumNightsClass { number = new double[] { Number }, HotelNights = new string[] { HotelNights } } });
            AddIntent(ExtendStayWithDate, Intent.ExtendStay, datetime: new DateTimeSpec[] { new DateTimeSpec("date", new string[] { Date.ToString(TimexDateFormat) }) });
            AddIntent(ExtendStayWithTime, Intent.ExtendStay, datetime: new DateTimeSpec[] { new DateTimeSpec("time", new string[] { LateCheckOutUtterances.Time.ToString(TimexTimeFormat) }) });
        }
    }
}
