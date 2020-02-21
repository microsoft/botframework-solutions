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
    public class SendEmailUtterances : BaseTestUtterances
    {
        public SendEmailUtterances()
        {
            this.Add(SendEmails, CreateIntent(SendEmails, intent: EmailLuis.Intent.SendEmail));
            this.Add(SendEmailToEmailAdress, CreateIntent(
                SendEmailToEmailAdress,
                intent: EmailLuis.Intent.SendEmail,
                emailAdress: new string[] { ContextStrings.TestEmailAdress }));
            this.Add(SendEmailToNobody, CreateIntent(
                SendEmailToNobody,
                intent: EmailLuis.Intent.SendEmail,
                contactName: new string[] { ContextStrings.Nobody }));
            this.Add(SendEmailToRecipient, CreateIntent(
                SendEmailToRecipient,
                intent: EmailLuis.Intent.SendEmail,
                contactName: new string[] { ContextStrings.TestRecipient }));
            this.Add(SendEmailToRecipientWithSubject, CreateIntent(
                SendEmailToRecipientWithSubject,
                intent: EmailLuis.Intent.SendEmail,
                contactName: new string[] { ContextStrings.TestRecipient },
                subject: new string[] { ContextStrings.TestSubject }));
            this.Add(SendEmailToRecipientWithSubjectAndContext, CreateIntent(
                SendEmailToRecipientWithSubjectAndContext,
                intent: EmailLuis.Intent.SendEmail,
                contactName: new string[] { ContextStrings.TestRecipient },
                subject: new string[] { ContextStrings.TestSubject },
                message: new string[] { ContextStrings.TestContent }));
            this.Add(SendEmailToMultiRecipient, CreateIntent(
                SendEmailToMultiRecipient,
                intent: EmailLuis.Intent.SendEmail,
                contactName: new string[] { ContextStrings.TestRecipient, ContextStrings.TestRecipientWithDup }));
            this.Add(SendEmailToDupRecipient, CreateIntent(
                SendEmailToDupRecipient,
                intent: EmailLuis.Intent.SendEmail,
                contactName: new string[] { ContextStrings.TestRecipientWithDup }));
        }

        public static string SendEmails { get; } = "Send Emails";

        public static string SendEmailToEmailAdress { get; } = "Send email to " + ContextStrings.TestEmailAdress;

        public static string SendEmailToNobody { get; } = "Send email to " + ContextStrings.Nobody;

        public static string SendEmailToRecipient { get; } = "Send email to " + ContextStrings.TestRecipient;

        public static string SendEmailToRecipientWithSubject { get; } = "Send email to " + ContextStrings.TestRecipient + " title is " + ContextStrings.TestSubject;

        public static string SendEmailToRecipientWithSubjectAndContext { get; } = "Send email to " + ContextStrings.TestRecipient + " title is " + ContextStrings.TestSubject + " saying that " + ContextStrings.TestContent;

        public static string SendEmailToMultiRecipient { get; } = "Send email to " + ContextStrings.TestRecipient + " and " + ContextStrings.TestRecipientWithDup;

        public static string SendEmailToDupRecipient { get; } = "Send email to " + ContextStrings.TestRecipientWithDup;

        public static string SendEmailActionName { get; } = "SendEmail";

        public static Activity SendEmailAction { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = SendEmailActionName,
            Value = JObject.FromObject(new EmailInfo()
            {
                Subject = ContextStrings.TestSubject,
                Content = ContextStrings.TestContent,
                Reciever = new List<string>() { ContextStrings.TestEmailAdress }
            })
        };
    }
}
