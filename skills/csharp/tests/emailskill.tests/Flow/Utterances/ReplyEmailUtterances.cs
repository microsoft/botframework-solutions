// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using EmailSkill.Models.Action;
using EmailSkill.Tests.Flow.Strings;
using Luis;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace EmailSkill.Tests.Flow.Utterances
{
    public class ReplyEmailUtterances : BaseTestUtterances
    {
        public ReplyEmailUtterances()
        {
            this.Add(ReplyEmails, CreateIntent(ReplyEmails, intent: EmailLuis.Intent.Reply));
            this.Add(ReplyEmailsWithContent, CreateIntent(
                ReplyEmailsWithContent,
                intent: EmailLuis.Intent.Reply,
                message: new string[] { ContextStrings.TestContent }));
            this.Add(ReplyEmailsWithSelection, CreateIntent(
               ReplyEmailsWithSelection,
               intent: EmailLuis.Intent.Reply,
               ordinal: new double[] { 2 }));
            this.Add(ReplyCurrentEmail, CreateIntent(ReplyCurrentEmail, intent: EmailLuis.Intent.Reply));
        }

        public static string ReplyEmails { get; } = "Reply an Email";

        public static string ReplyEmailsWithContent { get; } = "Reply an Email saying " + ContextStrings.TestContent;

        public static string ReplyEmailsWithSelection { get; } = "Reply the second Email";

        public static string ReplyCurrentEmail { get; } = "Reply the current Email";

        public static string ReplyEmailActionName { get; } = "ReplyEmail";

        public static Activity ReplyEmailAction { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = ReplyEmailActionName,
            Value = JObject.FromObject(new ReplyEmailInfo()
            {
                ReplyMessage = ContextStrings.TestContent
            })
        };
    }
}
