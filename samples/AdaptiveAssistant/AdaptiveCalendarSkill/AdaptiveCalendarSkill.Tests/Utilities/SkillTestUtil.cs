// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Solutions.Testing.Mocks;
using AdaptiveCalendarSkill.Tests.Utterances;

namespace AdaptiveCalendarSkill.Tests.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, AdaptiveCalendarSkillLuis.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, AdaptiveCalendarSkillLuis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static AdaptiveCalendarSkillLuis CreateIntent(string userInput, AdaptiveCalendarSkillLuis.Intent intent)
        {
            var result = new AdaptiveCalendarSkillLuis
            {
                Text = userInput,
                Intents = new Dictionary<AdaptiveCalendarSkillLuis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new AdaptiveCalendarSkillLuis._Entities
            {
                _instance = new AdaptiveCalendarSkillLuis._Entities._Instance()
            };

            return result;
        }
    }
}
