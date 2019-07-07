// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Responses.Reservation
{

    public class ReservationResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ConfirmCheckOut = "ConfirmCheckOut";
        public const string RetryConfirmCheckOut = "RetryConfirmCheckOut";
    }
}