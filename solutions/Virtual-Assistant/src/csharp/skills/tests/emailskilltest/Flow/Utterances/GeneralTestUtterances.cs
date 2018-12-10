using System;
using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using static Luis.General;

namespace EmailSkillTest.Flow.Utterances
{
    public class GeneralTestUtterances : Dictionary<string, General>
    {
        public GeneralTestUtterances()
        {
            this.Add(NextPage, CreateIntent(Intent.Next));
            this.Add(PreviousPage, CreateIntent(Intent.Previous));
            this.Add(Yes, CreateIntent(Intent.None));
            this.Add(No, CreateIntent(Intent.None));
        }

        public static double TopIntentScore { get; } = 0.9;

        public static string NextPage { get; } = "Next page";

        public static string PreviousPage { get; } = "Previous page";

        public static string Yes { get; } = "Yes";

        public static string No { get; } = "No";

        public General GetBaseNoneIntent()
        {
            var generalIntent = new General();
            generalIntent.Intents = new Dictionary<Intent, IntentScore>();
            generalIntent.Intents.Add(Intent.None, new IntentScore() { Score = TopIntentScore });

            return generalIntent;
        }

        protected General CreateIntent(Intent intent)
        {
            var generalIntent = new General();
            generalIntent.Intents = new Dictionary<Intent, IntentScore>();
            generalIntent.Intents.Add(intent, new IntentScore() { Score = TopIntentScore });
            generalIntent.Entities = new _Entities();

            return generalIntent;
        }
    }
}
