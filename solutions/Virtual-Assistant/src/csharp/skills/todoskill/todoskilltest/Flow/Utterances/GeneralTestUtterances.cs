using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace ToDoSkillTest.Flow.Utterances
{
    public class GeneralTestUtterances : Dictionary<string, General>
    {
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

        public static double TopIntentScore { get; } = 0.9;

        public static string ShowNext { get; } = "show next";

        public static string ShowPrevious { get; } = "show previous";

        public static string ReadMore { get; } = "read more";

        public General GetBaseNoneIntent()
        {
            return GetGeneralIntent();
        }

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
