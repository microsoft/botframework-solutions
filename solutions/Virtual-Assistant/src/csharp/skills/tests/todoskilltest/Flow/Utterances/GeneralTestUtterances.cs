using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace ToDoSkillTest.Flow.Utterances
{
    public class GeneralTestUtterances : Dictionary<string, General>
    {
        public static double TopIntentScore { get; } = 0.9;

        public General GetBaseNoneIntent()
        {
            var intent = new General();
            intent.Intents = new Dictionary<General.Intent, IntentScore>();
            intent.Intents.Add(General.Intent.None, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new General._Entities();
            return intent;
        }
    }
}
