// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace CalendarSkill.Dialogs.FindContact.Resources
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
		public const string ConfirmMultiplContactEmailSinglePage = "ConfirmMultiplContactEmailSinglePage";
		public const string ConfirmMultiplContactEmailMultiPage = "ConfirmMultiplContactEmailMultiPage";
		public const string UserNotFound = "UserNotFound";
		public const string UserNotFoundAgain = "UserNotFoundAgain";
		public const string BeforeSendingMessage = "BeforeSendingMessage";
		public const string AlreadyFirstPage = "AlreadyFirstPage";
		public const string AlreadyLastPage = "AlreadyLastPage";

    }
}