// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using EmailSkill.Models.Action;
using EmailSkill.Tests.Flow.Strings;
using Luis;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace EmailSkill.Tests.Flow.Utterances
{
    public class ForwardEmailUtterances : BaseTestUtterances
    {
        public ForwardEmailUtterances()
        {
            this.Add(ForwardEmails, CreateIntent(ForwardEmails, intent: EmailLuis.Intent.Forward));
            this.Add(ForwardEmailsToRecipient, CreateIntent(
                ForwardEmailsToRecipient,
                intent: EmailLuis.Intent.Forward,
                contactName: new string[] { ContextStrings.TestRecipient }));
            this.Add(ForwardEmailsToRecipientWithContent, CreateIntent(
                ForwardEmailsToRecipientWithContent,
                intent: EmailLuis.Intent.Forward,
                contactName: new string[] { ContextStrings.TestRecipient },
                message: new string[] { ContextStrings.TestContent }));
            this.Add(ForwardEmailsToSelection, CreateIntent(
                ForwardEmailsToSelection,
                intent: EmailLuis.Intent.Forward,
                ordinal: new double[] { 2 }));
            this.Add(ForwardCurrentEmail, CreateIntent(ForwardCurrentEmail, intent: EmailLuis.Intent.Forward));
        }

        public static string ForwardEmails { get; } = "Forward Email";

        public static string ForwardEmailsToRecipient { get; } = "Forward Email to " + ContextStrings.TestRecipient;

        public static string ForwardEmailsToRecipientWithContent { get; } = "Forward Email to " + ContextStrings.TestRecipient + " saying " + ContextStrings.TestContent;

        public static string ForwardEmailsToSelection { get; } = "Forward the second Email";

        public static string ForwardCurrentEmail { get; } = "Forward this Email";

        public static string ForwardEmailActionName { get; } = "ForwardEmail";

        public static Activity ForwardEmailAction { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = ForwardEmailActionName,
            Value = JObject.FromObject(new ForwardEmailInfo()
            {
                ForwardMessage = ContextStrings.TestContent,
                ForwardReciever = new List<string>() { ContextStrings.TestEmailAdress }
            })
        };
    }
}
