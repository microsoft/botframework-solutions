// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using $safeprojectname$.Mocks;
using $safeprojectname$.Utterances;

namespace $safeprojectname$.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, $ext_safeprojectname$Luis.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, $ext_safeprojectname$Luis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static $ext_safeprojectname$Luis CreateIntent(string userInput, $ext_safeprojectname$Luis.Intent intent)
        {
            var result = new $ext_safeprojectname$Luis
            {
                Text = userInput,
                Intents = new Dictionary<$ext_safeprojectname$Luis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new $ext_safeprojectname$Luis._Entities
            {
                _instance = new $ext_safeprojectname$Luis._Entities._Instance()
            };

            return result;
        }
    }
}
