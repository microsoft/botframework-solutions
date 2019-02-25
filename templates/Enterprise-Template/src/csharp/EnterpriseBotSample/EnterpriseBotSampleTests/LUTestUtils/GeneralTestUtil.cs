using EnterpriseBotSampleTests.Mocks;
using EnterpriseBotSampleTests.Utterances;
using Luis;
using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnterpriseBotSampleTests.LuisTestUtils
{
    public class GeneralTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { GeneralUtterances.Cancel, CreateIntent(GeneralUtterances.Cancel, General.Intent.Cancel) },
            { GeneralUtterances.Escalate, CreateIntent(GeneralUtterances.Escalate, General.Intent.Escalate) },
            { GeneralUtterances.FinishTask, CreateIntent(GeneralUtterances.FinishTask, General.Intent.FinishTask) },
            { GeneralUtterances.GoBack, CreateIntent(GeneralUtterances.GoBack, General.Intent.GoBack) },
            { GeneralUtterances.Help, CreateIntent(GeneralUtterances.Help, General.Intent.Help) },
            { GeneralUtterances.Repeat, CreateIntent(GeneralUtterances.Repeat, General.Intent.Repeat) },
            { GeneralUtterances.SelectAny, CreateIntent(GeneralUtterances.SelectAny, General.Intent.SelectAny) },
            { GeneralUtterances.SelectItem, CreateIntent(GeneralUtterances.SelectItem, General.Intent.SelectItem) },
            { GeneralUtterances.SelectNone, CreateIntent(GeneralUtterances.SelectNone, General.Intent.SelectNone) },
            { GeneralUtterances.ShowNext, CreateIntent(GeneralUtterances.ShowNext, General.Intent.ShowNext) },
            { GeneralUtterances.ShowPrevious, CreateIntent(GeneralUtterances.ShowPrevious, General.Intent.ShowPrevious) },
            { GeneralUtterances.StartOver, CreateIntent(GeneralUtterances.StartOver, General.Intent.StartOver) },
            { GeneralUtterances.Stop, CreateIntent(GeneralUtterances.Stop, General.Intent.Stop) },
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
