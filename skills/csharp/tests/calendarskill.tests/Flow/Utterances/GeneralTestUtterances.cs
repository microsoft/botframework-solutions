// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkill.Test.Flow.Utterances
{
    public class GeneralTestUtterances : Dictionary<string, General>
    {
        public GeneralTestUtterances()
        {
            Add(ChooseOne, GetNoneIntentWithNumber(1));
        }

        public static double TopIntentScore { get; } = 0.9;

        public static string ChooseOne { get; } = "First one";

        public static string UnknownIntent { get; } = "What's the weather?";

        public General GetBaseNoneIntent()
        {
            var intent = new General
            {
                Intents = new Dictionary<Luis.General.Intent, IntentScore>()
            };
            intent.Intents.Add(General.Intent.None, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new General._Entities();
            return intent;
        }

        public General GetNoneIntentWithNumber(int number)
        {
            var intent = new General
            {
                Intents = new Dictionary<Luis.General.Intent, IntentScore>()
            };
            intent.Intents.Add(General.Intent.None, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new General._Entities();
            intent.Entities.number = new double[] { number };
            return intent;
        }
    }
}
