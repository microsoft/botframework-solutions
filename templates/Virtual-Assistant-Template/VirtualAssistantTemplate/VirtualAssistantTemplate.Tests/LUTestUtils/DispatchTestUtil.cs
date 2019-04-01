using VirtualAssistantTemplate.Tests.Mocks;
using VirtualAssistantTemplate.Tests.Utterances;
using Luis;
using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualAssistantTemplate.Tests.LuisTestUtils
{
    public class DispatchTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { GeneralUtterances.Cancel, CreateIntent(GeneralUtterances.Cancel, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.Escalate, CreateIntent(GeneralUtterances.Escalate, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.FinishTask, CreateIntent(GeneralUtterances.FinishTask, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.GoBack, CreateIntent(GeneralUtterances.GoBack, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.Help, CreateIntent(GeneralUtterances.Help, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.Repeat, CreateIntent(GeneralUtterances.Repeat, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.SelectAny, CreateIntent(GeneralUtterances.SelectAny, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.SelectItem, CreateIntent(GeneralUtterances.SelectItem, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.SelectNone, CreateIntent(GeneralUtterances.SelectNone, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.ShowNext, CreateIntent(GeneralUtterances.ShowNext, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.ShowPrevious, CreateIntent(GeneralUtterances.ShowPrevious, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.StartOver, CreateIntent(GeneralUtterances.StartOver, DispatchLUIS.Intent.l_general) },
            { GeneralUtterances.Stop, CreateIntent(GeneralUtterances.Stop, DispatchLUIS.Intent.l_general) },
            { FaqUtterances.Overview, CreateIntent(FaqUtterances.Overview, DispatchLUIS.Intent.q_faq) },
            { ChitchatUtterances.Greeting, CreateIntent(ChitchatUtterances.Greeting, DispatchLUIS.Intent.q_chitchat) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, DispatchLUIS.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static DispatchLUIS CreateIntent(string userInput, DispatchLUIS.Intent intent)
        {
            var result = new DispatchLUIS
            {
                Text = userInput,
                Intents = new Dictionary<DispatchLUIS.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new DispatchLUIS._Entities
            {
                _instance = new DispatchLUIS._Entities._Instance()
            };

            return result;
        }
    }
}