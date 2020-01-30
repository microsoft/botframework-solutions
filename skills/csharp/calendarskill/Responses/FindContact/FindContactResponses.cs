// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Responses.FindContact
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class FindContactResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string PromptOneNameOneAddress = "PromptOneNameOneAddress";
        public const string ConfirmMultipleContactNameSinglePage = "ConfirmMultipleContactNameSinglePage";
        public const string ConfirmMultipleContactNameMultiPage = "ConfirmMultipleContactNameMultiPage";
        public const string ConfirmMultipleContactEmailSinglePage = "ConfirmMultipleContactEmailSinglePage";
        public const string ConfirmMultipleContactEmailMultiPage = "ConfirmMultipleContactEmailMultiPage";
        public const string UserNotFound = "UserNotFound";
        public const string UserNotFoundAgain = "UserNotFoundAgain";
        public const string BeforeSendingMessage = "BeforeSendingMessage";
        public const string AlreadyFirstPage = "AlreadyFirstPage";
        public const string AlreadyLastPage = "AlreadyLastPage";
        public const string NoAttendees = "NoAttendees";
        public const string AddMoreUserPrompt = "AddMoreUserPrompt";
        public const string AddMoreAttendees = "AddMoreAttendees";
        public const string FindMultipleContactNames = "FindMultipleContactNames";
        public const string FindMultipleEmails = "FindMultipleEmails";
        public const string EmailChoiceConfirmation = "EmailChoiceConfirmation";
        public const string AskForEmail = "AskForEmail";
    }
}
