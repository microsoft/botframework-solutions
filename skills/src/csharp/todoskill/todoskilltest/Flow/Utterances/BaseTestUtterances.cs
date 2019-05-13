using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace ToDoSkillTest.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, ToDoLuis>
    {
        public BaseTestUtterances()
        {
        }

        public static double TopIntentScore { get; } = 0.9;

        public ToDoLuis GetBaseNoneIntent()
        {
            return GetToDoIntent();
        }

        public ToDoLuis GetNoneIntent(string[] listType = null)
        {
            return GetToDoIntent(listType: listType);
        }

        protected ToDoLuis GetToDoIntent(
            string userInput = null,
            ToDoLuis.Intent intents = ToDoLuis.Intent.None,
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
            var intent = new ToDoLuis();
            intent.Text = userInput;
            intent.Intents = new Dictionary<ToDoLuis.Intent, IntentScore>();
            intent.Intents.Add(intents, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new ToDoLuis._Entities();
            intent.Entities._instance = new ToDoLuis._Entities._Instance();
            intent.Entities.ordinal = ordinal;
            intent.Entities.ContainsAll = containsAll;
            intent.Entities.ListType = listType;
            intent.Entities.TaskContent = taskContentML;
            intent.Entities.ShopContent = shopContent;
            intent.Entities.TaskContentPattern = taskContentPattern;
            intent.Entities.FoodOfGrocery = foodOfGrocery;
            intent.Entities.ShopVerb = shopVerb;

            return intent;
        }
    }
}
