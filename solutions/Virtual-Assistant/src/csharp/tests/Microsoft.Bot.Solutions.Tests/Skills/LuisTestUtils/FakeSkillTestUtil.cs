using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Fakes;
using Microsoft.Bot.Solutions.Tests.Skills.Utterances;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Tests.Skills.LuisTestUtils
{
    public class FakeSkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, FakeSkillLU.Intent.Sample) },
            { SampleDialogUtterances.Auth, CreateIntent(SampleDialogUtterances.Auth, FakeSkillLU.Intent.Auth) }
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, FakeSkillLU.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static FakeSkillLU CreateIntent(string userInput, FakeSkillLU.Intent intent)
        {
            var result = new FakeSkillLU
            {
                Text = userInput,
                Intents = new Dictionary<FakeSkillLU.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new FakeSkillLU._Entities
            {
                _instance = new FakeSkillLU._Entities._Instance()
            };

            return result;
        }
    }
}
