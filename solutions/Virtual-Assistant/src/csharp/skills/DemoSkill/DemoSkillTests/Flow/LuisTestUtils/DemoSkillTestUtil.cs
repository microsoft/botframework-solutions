using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Solutions.Testing.Fakes;
using DemoSkillTests.Flow.Utterances;
using System.Collections.Generic;

namespace DemoSkillTests.Flow.LuisTestUtils
{
    public class DemoSkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, DemoSkillLU.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, DemoSkillLU.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static DemoSkillLU CreateIntent(string userInput, DemoSkillLU.Intent intent)
        {
            var result = new DemoSkillLU
            {
                Text = userInput,
                Intents = new Dictionary<DemoSkillLU.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new DemoSkillLU._Entities
            {
                _instance = new DemoSkillLU._Entities._Instance()
            };

            return result;
        }
    }
}
