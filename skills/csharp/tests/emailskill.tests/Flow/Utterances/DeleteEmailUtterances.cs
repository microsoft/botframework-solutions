// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using Microsoft.Bot.Schema;

namespace EmailSkill.Tests.Flow.Utterances
{
    public class DeleteEmailUtterances : BaseTestUtterances
    {
        public DeleteEmailUtterances()
        {
            this.Add(DeleteEmails, CreateIntent(DeleteEmails, intent: EmailLuis.Intent.Delete));
            this.Add(DeleteEmailsWithSelection, CreateIntent(
               DeleteEmailsWithSelection,
               intent: EmailLuis.Intent.Delete,
               ordinal: new double[] { 2 }));
            this.Add(DeleteCurrentEmail, CreateIntent(DeleteCurrentEmail, intent: EmailLuis.Intent.Delete));
        }

        public static string DeleteEmails { get; } = "Delete an Email";

        public static string DeleteEmailsWithSelection { get; } = "Delete the second Email";

        public static string DeleteCurrentEmail { get; } = "Delete the current Email";

        public static string DeleteEmailActionName { get; } = "DeleteEmail";

        public static Activity DeleteEmailAction { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = DeleteEmailActionName
        };
    }
}
