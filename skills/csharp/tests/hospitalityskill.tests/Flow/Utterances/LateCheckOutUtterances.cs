// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using HospitalitySkill.Models;
using Microsoft.Bot.Builder.AI.Luis;
using static Luis.HospitalityLuis;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class LateCheckOutUtterances : HospitalityTestUtterances
    {
        public static readonly TimeSpan Time = new TimeSpan(14, 0, 0);

        public static readonly TimeSpan ExceededTime = new TimeSpan(18, 0, 0);

        public static readonly string LateCheckOut = "can i check out late";

        public static readonly string LateCheckOutWithTime = $"can i check out late to {Time.ToString()}";

        public static readonly string LateCheckOutWithExceededTime = $"can i check out late to {ExceededTime.ToString()}";

        public LateCheckOutUtterances()
        {
            AddIntent(LateCheckOut, Intent.LateCheckOut);
            AddIntent(LateCheckOutWithTime, Intent.LateCheckOut, datetime: new DateTimeSpec[] { new DateTimeSpec("time", new string[] { Time.ToString(TimexTimeFormat) }) });
            AddIntent(LateCheckOutWithExceededTime, Intent.LateCheckOut, datetime: new DateTimeSpec[] { new DateTimeSpec("time", new string[] { ExceededTime.ToString(TimexTimeFormat) }) });
        }
    }
}
