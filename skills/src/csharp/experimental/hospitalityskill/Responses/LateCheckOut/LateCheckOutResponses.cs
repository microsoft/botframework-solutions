﻿// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace HospitalitySkill.Responses.LateCheckOut
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class LateCheckOutResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string CheckAvailability = "CheckAvailability";
        public const string MoveCheckOutPrompt = "MoveCheckOutPrompt";
        public const string RetryMoveCheckOut = "RetryMoveCheckOut";
        public const string MoveCheckOutSuccess = "MoveCheckOutSuccess";
        public const string HasLateCheckOut = "HasLateCheckOut";
    }
}