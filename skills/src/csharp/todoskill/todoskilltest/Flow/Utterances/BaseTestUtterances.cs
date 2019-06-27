using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace ToDoSkillTest.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, todoLuis>
    {
        public BaseTestUtterances()
        {
        }

        public static double TopIntentScore { get; } = 0.9;

        public todoLuis GetBaseNoneIntent()
        {
            return GetToDoIntent();
        }

        public todoLuis GetNoneIntent(string[] listType = null)
        {
            return GetToDoIntent(listType: listType);
        }

        protected todoLuis GetToDoIntent(
            string userInput = null,
            todoLuis.Intent intents = todoLuis.Intent.None,
            double[] ordinal = null,
            double[] number = null,
            string[] containsAll = null,
            string[] listType = null,
            string[] taskContentML = null,
            string[] shopContent = null,
            string[] taskContentPattern = null,
            string[][] foodOfGrocery = null,
            string[][] shopVerb = null)
        {
            var intent = new todoLuis();
            intent.Text = userInput;
            intent.Intents = new Dictionary<todoLuis.Intent, IntentScore>();
            intent.Intents.Add(intents, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new todoLuis._Entities();
            intent.Entities._instance = new todoLuis._Entities._Instance();
            intent.Entities.ordinal = ordinal;
            intent.Entities.ContainsAll = containsAll;
            intent.Entities.ListType = listType;
            intent.Entities.TaskContent = taskContentML;
            intent.Entities.TaskContent_Any = taskContentPattern;
            intent.Entities.FoodOfGrocery = foodOfGrocery;
            intent.Entities.ShopVerb = shopVerb;

            return intent;
        }
    }
}
