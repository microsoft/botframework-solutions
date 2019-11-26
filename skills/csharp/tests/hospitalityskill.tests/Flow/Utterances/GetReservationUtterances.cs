// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using static Luis.HospitalityLuis;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class GetReservationUtterances : HospitalityTestUtterances
    {
        public static readonly string GetReservation = "what are my reservation details";

        public GetReservationUtterances()
        {
            AddIntent(GetReservation, Intent.GetReservationDetails);
        }
    }
}
