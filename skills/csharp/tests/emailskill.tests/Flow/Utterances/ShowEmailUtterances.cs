// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using EmailSkill.Tests.Flow.Strings;
using Luis;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace EmailSkill.Tests.Flow.Utterances
{
    public class ShowEmailUtterances : BaseTestUtterances
    {
        public ShowEmailUtterances()
        {
            this.Add(ReadMore, CreateIntent(ReadMore, intent: EmailLuis.Intent.ReadAloud));
            this.Add(ShowEmails, CreateIntent(ShowEmails, intent: EmailLuis.Intent.CheckMessages));
            this.Add(ShowEmailsFromTestRecipient, CreateIntent(
                ShowEmailsFromTestRecipient,
                intent: EmailLuis.Intent.CheckMessages,
                senderName: new string[] { ContextStrings.TestRecipient }));
        }

        public static string ReadMore { get; } = "Read more";

        public static string ShowEmails { get; } = "Show Emails";

        public static string ShowEmailsFromTestRecipient { get; } = "Show Emails from" + ContextStrings.TestRecipient;

        public static string EmailSummaryActionName { get; } = "EmailSummary";

        public static Activity EmailSummaryAction { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = EmailSummaryActionName
        };
    }
}
