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
            return GetGeneralIntent();
        }

        public GeneralTestUtterances()
        {
            this.Add(ShowNext, GetGeneralIntent(
                ShowNext,
                General.Intent.Next));

            this.Add(ShowPrevious, GetGeneralIntent(
                ShowPrevious,
                General.Intent.Previous));

            this.Add(ReadMore, GetGeneralIntent(
                ReadMore,
                General.Intent.ReadMore));
        }

        public static string ShowNext = "show next";

        public static string ShowPrevious = "show previous";

        public static string ReadMore = "read more";

        public General GetGeneralIntent(
            string userInput = null,
            General.Intent intents = General.Intent.None)
        {
            var intent = new General();
            intent.Text = userInput;
            intent.Intents = new Dictionary<General.Intent, IntentScore>();
            intent.Intents.Add(intents, new IntentScore() { Score = TopIntentScore });
            return intent;
        }
    }
}
