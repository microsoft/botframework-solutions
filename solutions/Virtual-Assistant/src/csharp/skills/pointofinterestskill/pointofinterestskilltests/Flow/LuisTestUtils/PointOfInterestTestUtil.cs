using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Fakes;
using PointOfInterestSkillTests.Flow.Utterances;
using System.Collections.Generic;

namespace PointOfInterestSkillTests.Flow.LuisTestUtils
{
    public class PointOfInterestTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { PointOfInterestDialogUtterances.CancelRoute, CreateIntent(PointOfInterestDialogUtterances.CancelRoute, PointOfInterest.Intent.NAVIGATION_CANCEL_ROUTE) },
            { PointOfInterestDialogUtterances.CancelRoute, CreateIntent(PointOfInterestDialogUtterances.CancelRoute, PointOfInterest.Intent.NAVIGATION_CANCEL_ROUTE) },
            { PointOfInterestDialogUtterances.CancelRoute, CreateIntent(PointOfInterestDialogUtterances.CancelRoute, PointOfInterest.Intent.NAVIGATION_CANCEL_ROUTE) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, PointOfInterest.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static PointOfInterest CreateIntent(string userInput, PointOfInterest.Intent intent)
        {
            var result = new PointOfInterest
            {
                Text = userInput,
                Intents = new Dictionary<PointOfInterest.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new PointOfInterest._Entities
            {
                _instance = new PointOfInterest._Entities._Instance()
            };

            return result;
        }
    }
}
