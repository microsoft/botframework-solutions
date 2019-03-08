using System.Collections.Generic;
using Experimental.Skills.Tests.Utterances;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Mocks;

namespace Experimental.Skills.Tests.LuisTestUtils
{
    public class NewsTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { ExperimentalUtterances.FindNews, CreateIntent(ExperimentalUtterances.FindNews, Luis.News.Intent.FindArticles) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Luis.News.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Luis.News CreateIntent(string userInput, Luis.News.Intent intent)
        {
            var result = new Luis.News
            {
                Text = userInput,
                Intents = new Dictionary<Luis.News.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new Luis.News._Entities
            {
                _instance = new Luis.News._Entities._Instance()
            };

            return result;
        }
    }
}
