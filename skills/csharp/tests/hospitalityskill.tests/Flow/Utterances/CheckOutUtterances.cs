// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using static Luis.HospitalityLuis;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class CheckOutUtterances : HospitalityTestUtterances
    {
        public static readonly string CheckOut = "can i check out";

        public CheckOutUtterances()
        {
            AddIntent(CheckOut, Intent.CheckOut);
        }
    }
}
