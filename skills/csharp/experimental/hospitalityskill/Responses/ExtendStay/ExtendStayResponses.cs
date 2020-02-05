// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Responses.ExtendStay
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class ExtendStayResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ExtendDatePrompt = "ExtendDatePrompt";
        public const string RetryExtendDate = "RetryExtendDate";
        public const string ConfirmExtendStay = "ConfirmExtendStay";
        public const string RetryConfirmExtendStay = "RetryConfirmExtendStay";
        public const string ExtendStaySuccess = "ExtendStaySuccess";
        public const string SameDayRequested = "SameDayRequested";
        public const string NotFutureDateError = "NotFutureDateError";
        public const string NumberEntityError = "NumberEntityError";
        public const string ConfirmAddNights = "ConfirmAddNights";
    }
}