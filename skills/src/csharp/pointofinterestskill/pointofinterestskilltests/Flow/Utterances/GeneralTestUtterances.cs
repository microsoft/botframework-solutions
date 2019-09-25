using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;
using static Luis.General;

namespace PointOfInterestSkillTests.Flow.Utterances
{
    public class GeneralTestUtterances : Dictionary<string, General>
    {
        public static string UnknownIntent { get; } = "what's the weather?";

        public static double TopIntentScore { get; } = 0.9;

        public General GetBaseNoneIntent()
        {
            var generalIntent = new General
            {
                Intents = new Dictionary<Intent, IntentScore>()
            };
            generalIntent.Intents.Add(Intent.None, new IntentScore() { Score = TopIntentScore });

            return generalIntent;
        }
    }
}
