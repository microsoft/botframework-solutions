// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;
using static Luis.General;

namespace PointOfInterestSkill.Tests.Flow.Utterances
{
    public class GeneralTestUtterances : Dictionary<string, General>
    {
        public GeneralTestUtterances()
        {
            AddIntent(BaseTestUtterances.No, Intent.Reject);
            AddIntent(SelectNone, Intent.SelectNone);
            AddIntent(Help, Intent.Help);
            AddIntent(Cancel, Intent.Cancel);
        }

        public static string UnknownIntent { get; } = "what's the weather?";

        public static string SelectNone { get; } = "none of these";

        public static string Help { get; } = "help";

        public static string Cancel { get; } = "cancel";

        public static double TopIntentScore { get; } = 0.9;

        public General GetBaseNoneIntent()
        {
            var generalIntent = new General
            {
                Intents = new Dictionary<Intent, IntentScore>()
            };
            generalIntent.Intents.Add(Intent.None, new IntentScore() { Score = TopIntentScore });

            return generalIntent;
        }

        protected void AddIntent(
            string userInput,
            Intent intent)
        {
            var generalIntent = new General
            {
                Text = userInput,
                Intents = new Dictionary<Intent, IntentScore>()
            };
            generalIntent.Intents.Add(intent, new IntentScore() { Score = TopIntentScore });

            Add(userInput, generalIntent);
        }
    }
}
