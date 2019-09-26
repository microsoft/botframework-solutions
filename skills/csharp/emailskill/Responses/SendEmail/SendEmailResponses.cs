// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace EmailSkill.Responses.SendEmail
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class SendEmailResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string NoSubject = "NoSubject";
        public const string NoMessageBody = "NoMessageBody";
        public const string RetryNoSubject = "RetryNoSubject";
        public const string PlayBackMessage = "PlayBackMessage";
        public const string CheckContent = "CheckContent";
        public const string RetryContent = "RetryContent";
        public const string GetRecreateInfo = "GetRecreateInfo";
        public const string GetRecreateInfoRetry = "GetRecreateInfoRetry";
        public const string ConfirmMessageRetry = "ConfirmMessageRetry";
    }
}