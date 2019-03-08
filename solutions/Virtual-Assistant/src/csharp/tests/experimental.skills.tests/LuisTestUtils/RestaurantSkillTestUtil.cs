using System.Collections.Generic;
using Experimental.Skills.Tests.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Mocks;

namespace Experimental.Skills.Tests.LuisTestUtils
{
    public class RestaurantSkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { ExperimentalUtterances.BookRestaurant, CreateIntent(ExperimentalUtterances.BookRestaurant, Luis.Reservation.Intent.Reservation) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Luis.Reservation.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Luis.Reservation CreateIntent(string userInput, Luis.Reservation.Intent intent)
        {
            var result = new Luis.Reservation
            {
                Text = userInput,
                Intents = new Dictionary<Luis.Reservation.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new Luis.Reservation._Entities
            {
                _instance = new Luis.Reservation._Entities._Instance()
            };

            return result;
        }
    }
}
