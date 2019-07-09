// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Responses.CheckOut
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class CheckOutResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ConfirmCheckOut = "ConfirmCheckOut";
        public const string RetryConfirmCheckOut = "RetryConfirmCheckOut";
        public const string CheckOutMessage = "CheckOutMessage";
        public const string ThankYou = "ThankYou";
        public const string HelpOtherwise = "HelpOtherwise";
    }
}