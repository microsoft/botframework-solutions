using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Mocks;
using $safeprojectname$.Flow.Utterances;
using System.Collections.Generic;

namespace $safeprojectname$.Flow.LuisTestUtils
{
    public class $ext_safeprojectname$TestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, $ext_safeprojectname$LU.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, $ext_safeprojectname$LU.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static $ext_safeprojectname$LU CreateIntent(string userInput, $ext_safeprojectname$LU.Intent intent)
        {
            var result = new $ext_safeprojectname$LU
            {
                Text = userInput,
                Intents = new Dictionary<$ext_safeprojectname$LU.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new $ext_safeprojectname$LU._Entities
            {
                _instance = new $ext_safeprojectname$LU._Entities._Instance()
            };

            return result;
        }
    }
}
