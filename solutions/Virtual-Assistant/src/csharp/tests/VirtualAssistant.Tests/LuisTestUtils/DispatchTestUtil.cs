using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Fakes;
using VirtualAssistant.Tests.Utterances;

namespace VirtualAssistant.Tests.LuisTestUtils
{
    public class DispatchTestUtil
    {
        /// <summary>
        /// Map utternaces to intents.
        /// </summary>
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            // General
            { GeneralUtterances.Cancel, CreateIntent(GeneralUtterances.Cancel, Dispatch.Intent.l_General) },
            { GeneralUtterances.Escalate, CreateIntent(GeneralUtterances.Escalate, Dispatch.Intent.l_General) },
            { GeneralUtterances.Goodbye, CreateIntent(GeneralUtterances.Goodbye, Dispatch.Intent.l_General) },
            { GeneralUtterances.Greeting, CreateIntent(GeneralUtterances.Greeting, Dispatch.Intent.l_General) },
            { GeneralUtterances.Help, CreateIntent(GeneralUtterances.Help, Dispatch.Intent.l_General) },
            { GeneralUtterances.Logout, CreateIntent(GeneralUtterances.Logout, Dispatch.Intent.l_General) },
            { GeneralUtterances.Next, CreateIntent(GeneralUtterances.Next, Dispatch.Intent.l_General) },
            { GeneralUtterances.Previous, CreateIntent(GeneralUtterances.Previous, Dispatch.Intent.l_General) },
            { GeneralUtterances.Restart, CreateIntent(GeneralUtterances.Restart, Dispatch.Intent.l_General) },

            // Calendar
            { CalendarUtterances.BookMeeting, CreateIntent(CalendarUtterances.BookMeeting, Dispatch.Intent.l_Calendar) },

            // Email
            { EmailUtterances.SendEmail, CreateIntent(EmailUtterances.SendEmail, Dispatch.Intent.l_Email) },

            // PoI
            { PointOfInterestUtterances.FindCoffeeShop, CreateIntent(PointOfInterestUtterances.FindCoffeeShop, Dispatch.Intent.l_PointOfInterest) },

            // ToDo
            { ToDoUtterances.AddToDo, CreateIntent(ToDoUtterances.AddToDo, Dispatch.Intent.l_ToDo) },
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
