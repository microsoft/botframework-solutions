// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Responses.RequestItem
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class RequestItemResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string ItemPrompt = "ItemPrompt";
        public const string RetryItemPrompt = "RetryItemPrompt";
        public const string ItemNotAvailable = "ItemNotAvailable";
        public const string GuestServicesPrompt = "GuestServicesPrompt";
        public const string RetryGuestServicesPrompt = "RetryGuestServicesPrompt";
        public const string GuestServicesConfirm = "GuestServicesConfirm";
        public const string ItemsRequested = "ItemsRequested";
    }
}