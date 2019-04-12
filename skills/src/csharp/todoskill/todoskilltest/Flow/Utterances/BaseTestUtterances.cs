using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace ToDoSkillTest.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, ToDoLU>
    {
        public BaseTestUtterances()
        {
        }

        public static double TopIntentScore { get; } = 0.9;

        public ToDoLU GetBaseNoneIntent()
        {
            return GetToDoIntent();
        }

        public ToDoLU GetNoneIntent(string[] listType = null)
        {
            return GetToDoIntent(listType: listType);
        }

        protected ToDoLU GetToDoIntent(
            string userInput = null,
            ToDoLU.Intent intents = ToDoLU.Intent.None,
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
            var intent = new ToDoLU();
            intent.Text = userInput;
            intent.Intents = new Dictionary<ToDoLU.Intent, IntentScore>();
            intent.Intents.Add(intents, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new ToDoLU._Entities();
            intent.Entities._instance = new ToDoLU._Entities._Instance();
            intent.Entities.ordinal = ordinal;
            intent.Entities.number = number;
            intent.Entities.ContainsAll = containsAll;
            intent.Entities.ListType = listType;
            intent.Entities.TaskContentML = taskContentML;
            intent.Entities.ShopContent = shopContent;
            intent.Entities.TaskContentPattern = taskContentPattern;
            intent.Entities.FoodOfGrocery = foodOfGrocery;
            intent.Entities.ShopVerb = shopVerb;

            return intent;
        }
    }
}
