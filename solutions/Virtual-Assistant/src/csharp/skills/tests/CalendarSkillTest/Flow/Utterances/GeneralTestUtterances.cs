using System;
using System.Collections.Generic;
using System.Text;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Utterances
{
    public class GeneralTestUtterances : Dictionary<string, General>
    {
        public static double TopIntentScore { get; } = 0.9;

        public General GetBaseNoneIntent()
        {
            var intent = new General
            {
                Intents = new Dictionary<Luis.General.Intent, IntentScore>()
            };
            intent.Intents.Add(General.Intent.None, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new General._Entities();
            return intent;
        }
    }
}
