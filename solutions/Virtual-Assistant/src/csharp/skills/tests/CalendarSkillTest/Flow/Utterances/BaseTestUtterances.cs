using System;
using System.Collections.Generic;
using System.Text;
using CalendarSkillTest.Flow.Fakes;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkillTest.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, Calendar>
    {
        public BaseTestUtterances()
        {
        }

        public static double TopIntentScore { get; } = 0.9;

        public Calendar GetBaseNoneIntent()
        {
            var intent = new Calendar();
            intent.Intents = new Dictionary<Luis.Calendar.Intent, IntentScore>();
            intent.Intents.Add(Calendar.Intent.None, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new Calendar._Entities();
            return intent;
        }
    }
}
