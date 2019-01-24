using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Fakes;
using VirtualAssistant.Tests.Utterances;

namespace VirtualAssistant.Tests.LuisTestUtils
{
    public class ToDoTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { ToDoUtterances.AddToDo, CreateIntent(ToDoUtterances.AddToDo, Luis.ToDo.Intent.AddToDo) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Luis.ToDo.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Luis.ToDo CreateIntent(string userInput, Luis.ToDo.Intent intent)
        {
            var result = new Luis.ToDo
            {
                Text = userInput,
                Intents = new Dictionary<Luis.ToDo.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new Luis.ToDo._Entities
            {
                _instance = new Luis.ToDo._Entities._Instance()
            };

            return result;
        }
    }
}
