// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace EmailSkill.Dialogs.Shared.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class EmailSharedResponses : IResponseIdCollection
    {
		public const string DidntUnderstandMessage = "DidntUnderstandMessage";
		public const string DidntUnderstandMessageIgnoringInput = "DidntUnderstandMessageIgnoringInput";
		public const string CancellingMessage = "CancellingMessage";
		public const string NoAuth = "NoAuth";
		public const string AuthFailed = "AuthFailed";
		public const string ActionEnded = "ActionEnded";
		public const string EmailErrorMessage = "EmailErrorMessage";
		public const string EmailErrorMessage_BotProblem = "EmailErrorMessage_BotProblem";
		public const string SentSuccessfully = "SentSuccessfully";
		public const string NoRecipients = "NoRecipients";
		public const string NoEmailContent = "NoEmailContent";
		public const string RecipientConfirmed = "RecipientConfirmed";
		public const string ConfirmSend = "ConfirmSend";
		public const string ConfirmSendFailed = "ConfirmSendFailed";
		public const string EmailNotFound = "EmailNotFound";
		public const string NoFocusMessage = "NoFocusMessage";
		public const string ShowEmailPrompt = "ShowEmailPrompt";
		public const string ShowOneEmailPrompt = "ShowOneEmailPrompt";
		public const string NoChoiceOptions_Retry = "NoChoiceOptions_Retry";    }
}