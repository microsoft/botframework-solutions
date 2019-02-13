using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Fakes;
using VirtualAssistant.Tests.Utterances;

namespace VirtualAssistant.Tests.LuisTestUtils
{
    public class EmailTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { EmailUtterances.SendEmail, CreateIntent(EmailUtterances.SendEmail, Luis.EmailLU.Intent.SendEmail) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Luis.EmailLU.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Luis.EmailLU CreateIntent(string userInput, Luis.EmailLU.Intent intent)
        {
            var result = new Luis.EmailLU
            {
                Text = userInput,
                Intents = new Dictionary<Luis.EmailLU.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new Luis.EmailLU._Entities
            {
                _instance = new Luis.EmailLU._Entities._Instance()
            };

            return result;
        }
    }
}
