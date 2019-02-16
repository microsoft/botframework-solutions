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
            { CalendarUtterances.BookMeeting, CreateIntent(CalendarUtterances.BookMeeting, Luis.CalendarLU.Intent.CreateCalendarEntry) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Luis.CalendarLU.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Luis.CalendarLU CreateIntent(string userInput, Luis.CalendarLU.Intent intent)
        {
            var result = new Luis.CalendarLU
            {
                Text = userInput,
                Intents = new Dictionary<Luis.CalendarLU.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new Luis.CalendarLU._Entities
            {
                _instance = new Luis.CalendarLU._Entities._Instance()
            };

            return result;
        }
    }
}
