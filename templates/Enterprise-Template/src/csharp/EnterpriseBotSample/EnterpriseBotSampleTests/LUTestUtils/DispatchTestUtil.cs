using EnterpriseBotSampleTests.Mocks;
using EnterpriseBotSampleTests.Utterances;
using Luis;
using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnterpriseBotSampleTests.LuisTestUtils
{
    public class DispatchTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { GeneralUtterances.Cancel, CreateIntent(GeneralUtterances.Cancel, Dispatch.Intent.l_general) },
            { GeneralUtterances.Escalate, CreateIntent(GeneralUtterances.Escalate, Dispatch.Intent.l_general) },
            { GeneralUtterances.FinishTask, CreateIntent(GeneralUtterances.FinishTask, Dispatch.Intent.l_general) },
            { GeneralUtterances.GoBack, CreateIntent(GeneralUtterances.GoBack, Dispatch.Intent.l_general) },
            { GeneralUtterances.Help, CreateIntent(GeneralUtterances.Help, Dispatch.Intent.l_general) },
            { GeneralUtterances.Repeat, CreateIntent(GeneralUtterances.Repeat, Dispatch.Intent.l_general) },
            { GeneralUtterances.SelectAny, CreateIntent(GeneralUtterances.SelectAny, Dispatch.Intent.l_general) },
            { GeneralUtterances.SelectItem, CreateIntent(GeneralUtterances.SelectItem, Dispatch.Intent.l_general) },
            { GeneralUtterances.SelectNone, CreateIntent(GeneralUtterances.SelectNone, Dispatch.Intent.l_general) },
            { GeneralUtterances.ShowNext, CreateIntent(GeneralUtterances.ShowNext, Dispatch.Intent.l_general) },
            { GeneralUtterances.ShowPrevious, CreateIntent(GeneralUtterances.ShowPrevious, Dispatch.Intent.l_general) },
            { GeneralUtterances.StartOver, CreateIntent(GeneralUtterances.StartOver, Dispatch.Intent.l_general) },
            { GeneralUtterances.Stop, CreateIntent(GeneralUtterances.Stop, Dispatch.Intent.l_general) },
            { FaqUtterances.Overview, CreateIntent(FaqUtterances.Overview, Dispatch.Intent.q_faq) },
            { ChitchatUtterances.Greeting, CreateIntent(ChitchatUtterances.Greeting, Dispatch.Intent.q_chitchat) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Dispatch.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Dispatch CreateIntent(string userInput, Dispatch.Intent intent)
        {
            var result = new Dispatch
            {
                Text = userInput,
                Intents = new Dictionary<Dispatch.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new Dispatch._Entities
            {
                _instance = new Dispatch._Entities._Instance()
            };

            return result;
        }
    }
}