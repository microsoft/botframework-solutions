// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using SkillSample.Tests.Mocks;
using SkillSample.Tests.Utterances;

namespace SkillSample.Tests.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, SkillSampleLuis.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, SkillSampleLuis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static SkillSampleLuis CreateIntent(string userInput, SkillSampleLuis.Intent intent)
        {
            var result = new SkillSampleLuis
            {
                Text = userInput,
                Intents = new Dictionary<SkillSampleLuis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new SkillSampleLuis._Entities
            {
                _instance = new SkillSampleLuis._Entities._Instance()
            };

            return result;
        }
    }
}
