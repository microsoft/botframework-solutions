// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using FoodOrderSkill.Tests.Mocks;
using FoodOrderSkill.Tests.Utterances;

namespace FoodOrderSkill.Tests.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, FoodOrderSkillLuis.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, FoodOrderSkillLuis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static FoodOrderSkillLuis CreateIntent(string userInput, FoodOrderSkillLuis.Intent intent)
        {
            var result = new FoodOrderSkillLuis
            {
                Text = userInput,
                Intents = new Dictionary<FoodOrderSkillLuis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new FoodOrderSkillLuis._Entities
            {
                _instance = new FoodOrderSkillLuis._Entities._Instance()
            };

            return result;
        }
    }
}
