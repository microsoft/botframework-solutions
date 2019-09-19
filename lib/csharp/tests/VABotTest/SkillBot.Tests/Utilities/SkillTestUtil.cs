// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Solutions.Testing.Mocks;
using SkillBot.Tests.Utterances;

namespace SkillBot.Tests.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, SkillBotLuis.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, SkillBotLuis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static SkillBotLuis CreateIntent(string userInput, SkillBotLuis.Intent intent)
        {
            var result = new SkillBotLuis
            {
                Text = userInput,
                Intents = new Dictionary<SkillBotLuis.Intent, IntentScore>(),
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new SkillBotLuis._Entities
            {
                _instance = new SkillBotLuis._Entities._Instance(),
            };

            return result;
        }
    }
}
