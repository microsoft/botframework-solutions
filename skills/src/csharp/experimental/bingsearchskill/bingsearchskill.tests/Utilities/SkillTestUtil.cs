using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Solutions.Testing.Mocks;
using BingSearchSkill.Tests.Utterances;
using System.Collections.Generic;

namespace BingSearchSkill.Tests.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, BingSearchSkillLuis.Intent.SearchMovieInfo) },
            //{ SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, BingSearchSkillLuis.Intent.GetCelebrityInfo) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, BingSearchSkillLuis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static BingSearchSkillLuis CreateIntent(string userInput, BingSearchSkillLuis.Intent intent)
        {
            var result = new BingSearchSkillLuis
            {
                Text = userInput,
                Intents = new Dictionary<BingSearchSkillLuis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new BingSearchSkillLuis._Entities
            {
                _instance = new BingSearchSkillLuis._Entities._Instance()
            };

            return result;
        }
    }
}
