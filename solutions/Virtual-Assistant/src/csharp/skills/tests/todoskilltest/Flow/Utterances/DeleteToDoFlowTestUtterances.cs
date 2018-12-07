using Luis;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkillTest.Flow.Utterances
{
    public class DeleteToDoFlowTestUtterances : ShowToDoFlowTestUtterances
    {
        public DeleteToDoFlowTestUtterances()
        {
            var number = new double[] { 1 };
            this.Add(DeleteSpecificTask, GetBaseDeleteToDoIntent(
                DeleteSpecificTask,
                number: number));

            var listType = new string[] { ToDoStrings.Shopping };
            number = new double[] { 2 };
            this.Add(DeleteSpecificTaskWithListType, GetBaseDeleteToDoIntent(
                DeleteSpecificTaskWithListType,
                listType: listType,
                number: number));

            var containsAll = new string[] { "all" };
            this.Add(DeleteAllTasks, GetBaseDeleteToDoIntent(
                DeleteAllTasks,
                containsAll: containsAll));
        }

        public static string DeleteSpecificTask { get; } = "delete the first task";

        public static string DeleteSpecificTaskWithListType { get; } = "delete the second task in my shopping list";

        public static string DeleteAllTasks { get; } = "remove all my tasks";

        private ToDo GetBaseDeleteToDoIntent(
            string userInput,
            ToDo.Intent intents = ToDo.Intent.DeleteToDo,
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
