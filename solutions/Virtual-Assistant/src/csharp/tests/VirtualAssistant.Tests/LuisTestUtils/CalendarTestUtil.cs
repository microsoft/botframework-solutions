using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Fakes;
using VirtualAssistant.Tests.Utterances;

namespace VirtualAssistant.Tests.LuisTestUtils
{
    public class CalendarTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { CalendarUtterances.BookMeeting, CreateIntent(CalendarUtterances.BookMeeting, Luis.Calendar.Intent.CreateCalendarEntry) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Luis.Calendar.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Luis.Calendar CreateIntent(string userInput, Luis.Calendar.Intent intent)
        {
            var result = new Luis.Calendar
            {
                Text = userInput,
                Intents = new Dictionary<Luis.Calendar.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new Luis.Calendar._Entities
            {
                _instance = new Luis.Calendar._Entities._Instance()
            };

            return result;
        }
    }
}
