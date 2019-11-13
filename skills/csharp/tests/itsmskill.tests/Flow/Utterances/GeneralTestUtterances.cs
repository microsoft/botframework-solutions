// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using static Luis.GeneralLuis;

namespace ITSMSkill.Tests.Flow.Utterances
{
    public class GeneralTestUtterances : BaseTestUtterances<GeneralLuis>
    {
        public static readonly string Cancel = "cancel";

        public static readonly string Help = "help";

        public static readonly string Logout = "log out";

        public static readonly string None = "hello";

        public static readonly string Confirm = "yeah go ahead";

        public static readonly string Reject = "negative";

        public GeneralTestUtterances()
        {
            AddIntent(Cancel, Intent.Cancel);
            AddIntent(Help, Intent.Help);
            AddIntent(Logout, Intent.Logout);
            AddIntent(Confirm, Intent.Confirm);
            AddIntent(Reject, Intent.Reject);
        }

        public override GeneralLuis NoneIntent { get; } = new GeneralLuis
        {
            Intents = new Dictionary<Intent, IntentScore>
            {
                { Intent.None, new IntentScore() { Score = TopIntentScore } }
            }
        };

        protected void AddIntent(
            string userInput,
            Intent intent)
        {
            var generalIntent = new GeneralLuis
            {
                Text = userInput,
                Intents = new Dictionary<Intent, IntentScore>
                {
                    { intent, new IntentScore() { Score = TopIntentScore } }
                }
            };

            Add(userInput, generalIntent);
        }
    }
}
