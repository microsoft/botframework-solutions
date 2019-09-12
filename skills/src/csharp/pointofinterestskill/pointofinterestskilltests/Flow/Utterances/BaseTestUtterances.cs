using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;
using PointOfInterestSkillTests.Flow.Strings;
using static Luis.PointOfInterestLuis;

namespace PointOfInterestSkillTests.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, PointOfInterestLuis>
    {
        public static double TopIntentScore { get; } = 0.9;

        public static string LocationEvent { get; } = "/event:{ \"Name\": \"Location\", \"Value\": \"47.639620,-122.130610\" }";

        public static string OptionOne { get; } = "option 1";

        public static string OptionTwo { get; } = "option 2";

        public static string Yes { get; } = "yes";

        public static string No { get; } = "no";

        public static string Call { get; } = "call";

        public static string ShowDirections { get; } = "show directions";

        public static string StartNavigation { get; } = "start navigation";

        public void AddManager(BaseTestUtterances utterances)
        {
            foreach (var item in utterances)
            {
                if (!this.ContainsKey(item.Key))
                {
                    this.Add(item.Key, item.Value);
                }
            }
        }

        public PointOfInterestLuis GetBaseNoneIntent()
        {
            var emailIntent = new PointOfInterestLuis
            {
                Intents = new Dictionary<Intent, IntentScore>()
            };
            emailIntent.Intents.Add(Intent.None, new IntentScore() { Score = TopIntentScore });

            return emailIntent;
        }

        protected PointOfInterestLuis CreateIntent(
            string userInput,
            Intent intent,
            string[] keyword = null,
            string[][] poiType = null)
        {
            var poiIntent = new PointOfInterestLuis
            {
                Text = userInput,
                Intents = new Dictionary<Intent, IntentScore>()
            };
            poiIntent.Intents.Add(intent, new IntentScore() { Score = TopIntentScore });

            poiIntent.Entities = new _Entities
            {
                Keyword = keyword,
                PoiDescription = poiType
            };

            return poiIntent;
        }
    }
}
