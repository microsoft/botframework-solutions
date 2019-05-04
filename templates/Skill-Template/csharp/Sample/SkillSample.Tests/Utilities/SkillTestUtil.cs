// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Solutions.Testing.Mocks;
using SkillSample.Tests.Utterances;

namespace SkillSample.Tests.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, SkillLuis.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, SkillLuis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static SkillLuis CreateIntent(string userInput, SkillLuis.Intent intent)
        {
            var result = new SkillLuis
            {
                Text = userInput,
                Intents = new Dictionary<SkillLuis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new SkillLuis._Entities
            {
                _instance = new SkillLuis._Entities._Instance()
            };

            return result;
        }
    }
}
