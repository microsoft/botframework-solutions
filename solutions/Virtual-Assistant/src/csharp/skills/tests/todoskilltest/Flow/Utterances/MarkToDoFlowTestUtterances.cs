using Luis;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkillTest.Flow.Utterances
{
    public class MarkToDoFlowTestUtterances : ShowToDoFlowTestUtterances
    {
        public MarkToDoFlowTestUtterances()
        {
            var number = new double[] { 2 };
            this.Add(MarkTaskAsCompleted, GetBaseMarkToDoIntent(
                MarkTaskAsCompleted,
                number: number));

            var listType = new string[] { ToDoStrings.Grocery };
            number = new double[] { 3 };
            this.Add(MarkTaskAsCompletedWithListType, GetBaseMarkToDoIntent(
                MarkTaskAsCompletedWithListType,
                listType: listType,
                number: number));

            var containsAll = new string[] { "all" };
            this.Add(MarkAllTasksAsCompleted, GetBaseMarkToDoIntent(
                MarkAllTasksAsCompleted,
                containsAll: containsAll));
        }

        public static string MarkTaskAsCompleted { get; } = "mark the second task as completed";

        public static string MarkTaskAsCompletedWithListType { get; } = "mark task three in my grocery list as completed";

        public static string MarkAllTasksAsCompleted { get; } = "mark all tasks as completed";

        private ToDo GetBaseMarkToDoIntent(
            string userInput,
            ToDo.Intent intents = ToDo.Intent.MarkToDo,
            double[] ordinal = null,
            double[] number = null,
            string[] listType = null,
            string[] containsAll = null)
        {
            return GetToDoIntent(
                userInput,
                intents,
                ordinal: ordinal,
                number: number,
                listType: listType,
                containsAll: containsAll);
        }
    }
}
