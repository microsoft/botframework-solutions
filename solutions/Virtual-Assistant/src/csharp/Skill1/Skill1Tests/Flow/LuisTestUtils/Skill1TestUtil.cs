using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Fakes;
using Skill1Tests.Flow.Utterances;
using System.Collections.Generic;

namespace Skill1Tests.Flow.LuisTestUtils
{
    public class Skill1TestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, Skill1LU.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Skill1LU.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Skill1LU CreateIntent(string userInput, Skill1LU.Intent intent)
        {
            var result = new Skill1LU
            {
                Text = userInput,
                Intents = new Dictionary<Skill1LU.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new Skill1LU._Entities
            {
                _instance = new Skill1LU._Entities._Instance()
            };

            return result;
        }
    }
}
