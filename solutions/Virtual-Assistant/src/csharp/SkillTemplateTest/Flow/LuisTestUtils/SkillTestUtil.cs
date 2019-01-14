using Luis;
using Microsoft.Bot.Builder;
using SkillTemplate.Tests.Flow.Fakes;
using SkillTemplate.Tests.Flow.Utterances;
using System.Collections.Generic;

namespace SkillTemplate.Tests.Flow.LuisTestUtils
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, Skill.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, Skill.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static Skill CreateIntent(string userInput, Skill.Intent intent)
        {
            var skillIntent = new Skill
            {
                Text = userInput,
                Intents = new Dictionary<Skill.Intent, IntentScore>()
            };

            skillIntent.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            skillIntent.Entities = new Skill._Entities
            {
                _instance = new Skill._Entities._Instance()
            };

            return skillIntent;
        }
    }
}
