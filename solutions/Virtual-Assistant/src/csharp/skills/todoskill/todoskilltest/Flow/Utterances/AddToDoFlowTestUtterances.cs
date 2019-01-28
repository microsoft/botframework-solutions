using Luis;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Flow.Utterances
{
    public class AddToDoFlowTestUtterances : BaseTestUtterances
    {
        public AddToDoFlowTestUtterances()
        {
            this.Add(BaseAddTask, GetBaseAddTaskIntent(BaseAddTask));
            this.Add(TaskContent, GetBaseNoneIntent());

            var shopContent = new string[] { "eggs" };
            var foodOfGrocery = new string[1][];
            foodOfGrocery[0] = new string[1];
            foodOfGrocery[0][0] = "eggs";
            var taskContentML = new string[] { "buy eggs" };
            var shopVerb = new string[1][];
            shopVerb[0] = new string[1];
            shopVerb[0][0] = "buy";
            this.Add(AddTaskWithContent, GetBaseAddTaskIntent(
                AddTaskWithContent,
                shopContent: shopContent,
                foodOfGrocery: foodOfGrocery,
                taskContentML: taskContentML,
                shopVerb: shopVerb));

            var listType = new string[] { MockData.Grocery };
            var taskContentPattern = new string[] { "eggs" };
            taskContentML = new string[] { "eggs" };
            foodOfGrocery[0][0] = "eggs";

            this.Add(AddTaskWithContentAndListType, GetBaseAddTaskIntent(
                AddTaskWithContentAndListType,
                listType: listType,
                taskContentPattern: taskContentPattern,
                taskContentML: taskContentML,
                foodOfGrocery: foodOfGrocery));

            listType = new string[] { MockData.Shopping };
            taskContentPattern = new string[] { "shoes" };
            shopVerb[0][0] = "purchase";
            taskContentML = new string[] { "shoes" };

            this.Add(AddTaskWithContentAndShopVerb, GetBaseAddTaskIntent(
                AddTaskWithContentAndShopVerb,
                listType: listType,
                shopVerb: shopVerb,
                taskContentML: taskContentML));
        }

        public static string BaseAddTask { get; } = "add a task";

        public static string TaskContent { get; } = "call my mother";

        public static string AddTaskWithContent { get; } = "remind me to buy eggs";

        public static string AddTaskWithContentAndListType { get; } = "add eggs to my grocery list";

        public static string AddTaskWithContentAndShopVerb { get; } = "add purchase shoes to my shopping list";

        private ToDo GetBaseAddTaskIntent(
            string userInput,
            ToDo.Intent intents = ToDo.Intent.AddToDo,
            string[] listType = null,
            string[] taskContentML = null,
            string[] shopContent = null,
            string[] taskContentPattern = null,
            string[][] foodOfGrocery = null,
            string[][] shopVerb = null)
        {
            return GetToDoIntent(
                userInput,
                intents,
                listType: listType,
                taskContentML: taskContentML,
                shopContent: shopContent,
                taskContentPattern: taskContentPattern,
                foodOfGrocery: foodOfGrocery,
                shopVerb: shopVerb);
        }
    }
}
