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
            { EmailUtterances.SendEmail, CreateIntent(EmailUtterances.SendEmail, Luis.Email.Intent.SendEmail) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Luis.Email.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Luis.Email CreateIntent(string userInput, Luis.Email.Intent intent)
        {
            var result = new Luis.Email
            {
                Text = userInput,
                Intents = new Dictionary<Luis.Email.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new Luis.Email._Entities
            {
                _instance = new Luis.Email._Entities._Instance()
            };

            return result;
        }
    }
}
