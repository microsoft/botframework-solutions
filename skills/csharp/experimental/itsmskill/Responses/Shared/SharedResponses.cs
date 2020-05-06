// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Responses;

namespace ITSMSkill.Responses.Shared
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class SharedResponses : IResponseIdCollection
    {
        // Generated accessors
        public const string DidntUnderstandMessage = "DidntUnderstandMessage";
        public const string CancellingMessage = "CancellingMessage";
        public const string NoAuth = "NoAuth";
        public const string AuthFailed = "AuthFailed";
        public const string ActionEnded = "ActionEnded";
        public const string ErrorMessage = "ErrorMessage";
        public const string ConfirmSearch = "ConfirmSearch";
        public const string InputSearch = "InputSearch";
        public const string ConfirmTitle = "ConfirmTitle";
        public const string InputTitle = "InputTitle";
        public const string ConfirmDescription = "ConfirmDescription";
        public const string InputDescription = "InputDescription";
        public const string ConfirmReason = "ConfirmReason";
        public const string InputReason = "InputReason";
        public const string ConfirmUrgency = "ConfirmUrgency";
        public const string InputUrgency = "InputUrgency";
        public const string ConfirmState = "ConfirmState";
        public const string InputState = "InputState";
        public const string ConfirmId = "ConfirmId";
        public const string InputId = "InputId";
        public const string InputTicketNumber = "InputTicketNumber";
        public const string PageIndicator = "PageIndicator";
        public const string ResultIndicator = "ResultIndicator";
        public const string ResultsIndicator = "ResultsIndicator";
        public const string ServiceFailed = "ServiceFailed";
        public const string SignOut = "SignOut";
    }
}