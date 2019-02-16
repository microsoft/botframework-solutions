using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Fakes;
using VirtualAssistant.Tests.Utterances;

namespace VirtualAssistant.Tests.LuisTestUtils
{
    public class PointOfInterestTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { PointOfInterestUtterances.FindCoffeeShop, CreateIntent(PointOfInterestUtterances.FindCoffeeShop, Luis.PointOfInterestLU.Intent.NAVIGATION_FIND_POINTOFINTEREST) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Luis.PointOfInterestLU.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Luis.PointOfInterestLU CreateIntent(string userInput, Luis.PointOfInterestLU.Intent intent)
        {
            var result = new Luis.PointOfInterestLU
            {
                Text = userInput,
                Intents = new Dictionary<Luis.PointOfInterestLU.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new Luis.PointOfInterestLU._Entities
            {
                _instance = new Luis.PointOfInterestLU._Entities._Instance()
            };

            return result;
        }
    }
}
