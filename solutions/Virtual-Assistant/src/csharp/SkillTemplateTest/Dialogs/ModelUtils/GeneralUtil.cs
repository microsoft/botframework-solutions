using Luis;
using Microsoft.Bot.Builder;
using System.Collections.Generic;

namespace SkillTemplateTest.Dialogs.Utilities
{
    public class GeneralUtil
    {
        protected General CreateIntent(
            string userInput,
            General.Intent intent)
        {
            var generalIntent = new General
            {
                Text = userInput,
                Intents = new Dictionary<General.Intent, IntentScore>()
            };

            generalIntent.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            generalIntent.Entities = new General._Entities
            {
                _instance = new General._Entities._Instance()
            };

            return generalIntent;
        }
    }
}
