using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Fakes;
using TestSkillTests.Flow.Utterances;
using System.Collections.Generic;

namespace TestSkillTests.Flow.LuisTestUtils
{
    public class TestSkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, TestSkillLU.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, TestSkillLU.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static TestSkillLU CreateIntent(string userInput, TestSkillLU.Intent intent)
        {
            var result = new TestSkillLU
            {
                Text = userInput,
                Intents = new Dictionary<TestSkillLU.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new TestSkillLU._Entities
            {
                _instance = new TestSkillLU._Entities._Instance()
            };

            return result;
        }
    }
}
