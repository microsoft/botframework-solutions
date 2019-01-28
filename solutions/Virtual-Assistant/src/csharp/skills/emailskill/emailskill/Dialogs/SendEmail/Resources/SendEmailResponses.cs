// https://docs.microsoft.com/en-us/visualstudio/modeling/t4-include-directive?view=vs-2017
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions.Resources;

namespace EmailSkill.Dialogs.SendEmail.Resources
{
    /// <summary>
    /// Contains bot responses.
    /// </summary>
    public class SendEmailResponses : IResponseIdCollection
    {
        // Generated accessors
		public const string RecipientConfirmed = "RecipientConfirmed";
		public const string NoSubject = "NoSubject";
		public const string NoMessageBody = "NoMessageBody";

    }
}