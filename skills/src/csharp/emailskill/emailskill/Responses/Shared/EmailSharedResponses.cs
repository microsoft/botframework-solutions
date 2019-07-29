// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Solutions.Responses;

namespace EmailSkill.Responses.Shared
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class EmailSharedResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string DidntUnderstandMessage = "DidntUnderstandMessage";
        public const string DidntUnderstandMessageIgnoringInput = "DidntUnderstandMessageIgnoringInput";
        public const string CancellingMessage = "CancellingMessage";
        public const string NoAuth = "NoAuth";
        public const string AuthFailed = "AuthFailed";
        public const string ActionEnded = "ActionEnded";
        public const string EmailErrorMessage = "EmailErrorMessage";
        public const string EmailErrorMessageBotProblem = "EmailErrorMessageBotProblem";
        public const string EmailErrorMessageAccountProblem = "EmailErrorMessageAccountProblem";
        public const string SentSuccessfully = "SentSuccessfully";
        public const string NoEmailContent = "NoEmailContent";
        public const string RecipientConfirmed = "RecipientConfirmed";
        public const string ConfirmSend = "ConfirmSend";
        public const string ConfirmSendMessage = "ConfirmSendMessage";
        public const string ConfirmSendRecipientsMessage = "ConfirmSendRecipientsMessage";
        public const string ConfirmSendRecipients = "ConfirmSendRecipients";
        public const string ConfirmSendRecipientsFailed = "ConfirmSendRecipientsFailed";
        public const string ConfirmSendFailed = "ConfirmSendFailed";
        public const string EmailNotFound = "EmailNotFound";
        public const string NoFocusMessage = "NoFocusMessage";
        public const string ShowEmailPrompt = "ShowEmailPrompt";
        public const string ShowEmailPromptOtherPage = "ShowEmailPromptOtherPage";
        public const string ShowOneEmailPrompt = "ShowOneEmailPrompt";
        public const string ShowOneEmailPromptOtherPage = "ShowOneEmailPromptOtherPage";
        public const string FirstPageAlready = "FirstPageAlready";
        public const string LastPageAlready = "LastPageAlready";
        public const string NoChoiceOptionsRetry = "NoChoiceOptionsRetry";
        public const string NoRecipients = "NoRecipients";
        public const string RetryInput = "RetryInput";
    }
}
