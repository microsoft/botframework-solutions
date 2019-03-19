using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Solutions.Testing.Mocks;
using $safeprojectname$.Flow.Utterances;
using System.Collections.Generic;

namespace $safeprojectname$.Flow.LuisTestUtils
{
    public class GeneralTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { GeneralUtterances.Cancel, CreateIntent(GeneralUtterances.Cancel, General.Intent.Cancel) },
            { GeneralUtterances.Escalate, CreateIntent(GeneralUtterances.Escalate, General.Intent.Escalate) },
            { GeneralUtterances.Help, CreateIntent(GeneralUtterances.Help, General.Intent.Help) },
            { GeneralUtterances.Logout, CreateIntent(GeneralUtterances.Logout, General.Intent.Logout) },
            { GeneralUtterances.Next, CreateIntent(GeneralUtterances.Next, General.Intent.ShowNext) },
            { GeneralUtterances.Previous, CreateIntent(GeneralUtterances.Previous, General.Intent.ShowPrevious) },
            { GeneralUtterances.Restart, CreateIntent(GeneralUtterances.Restart, General.Intent.StartOver) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, General.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static General CreateIntent(string userInput, General.Intent intent)
        {
            var result = new General
            {
                Text = userInput,
                Intents = new Dictionary<General.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new General._Entities
            {
                _instance = new General._Entities._Instance()
            };

            return result;
        }
    }
}
