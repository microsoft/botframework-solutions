using Luis;
using ToDoSkillTest.Flow.Fakes;

namespace ToDoSkillTest.Flow.Utterances
{
    public class MarkToDoFlowTestUtterances : BaseTestUtterances
    {
        public MarkToDoFlowTestUtterances()
        {
            this.Add(BaseMarkTask, GetBaseMarkToDoIntent(
                BaseMarkTask));

            this.Add(TaskContent, GetBaseNoneIntent());

            var ordinal = new double[] { 2 };
            this.Add(MarkSpecificTaskAsCompleted, GetBaseMarkToDoIntent(
                MarkSpecificTaskAsCompleted,
                ordinal: ordinal));

            var listType = new string[] { MockData.Grocery };
            ordinal = new double[] { 3 };
            this.Add(MarkSpecificTaskAsCompletedWithListType, GetBaseMarkToDoIntent(
                MarkSpecificTaskAsCompletedWithListType,
                listType: listType,
                ordinal: ordinal));

            ordinal = new double[1];
            var taskContentML = new string[] { "play games 1" };
            var taskContentPattern = new string[] { "play games 2" };
            this.Add(MarkTaskAsCompletedByContent, GetBaseMarkToDoIntent(
                MarkTaskAsCompletedByContent,
                ordinal: ordinal,
                taskContentML: taskContentML,
                taskContentPattern: taskContentPattern));

            var containsAll = new string[] { "all" };
            this.Add(MarkAllTasksAsCompleted, GetBaseMarkToDoIntent(
                MarkAllTasksAsCompleted,
                containsAll: containsAll));

            listType = new string[] { MockData.ToDo };
            this.Add(ConfirmListType, GetNoneIntent(
                listType: listType));
        }

        public static string BaseMarkTask { get; } = "mark a task as done";

        public static string TaskContent { get; } = "Play Games 1";

        public static string MarkSpecificTaskAsCompleted { get; } = "mark the second task as completed";

        public static string MarkSpecificTaskAsCompletedWithListType { get; } = "mark task three in my grocery list as completed";

        public static string MarkTaskAsCompletedByContent { get; } = "mark the task Play Games 1 as completed";

        public static string MarkAllTasksAsCompleted { get; } = "mark all tasks as completed";

        public static string ConfirmListType { get; } = "To Do list";

        private ToDoLuis GetBaseMarkToDoIntent(
            string userInput,
            ToDoLuis.Intent intents = ToDoLuis.Intent.MarkToDo,
            double[] ordinal = null,
            double[] number = null,
            string[] listType = null,
            string[] containsAll = null,
            string[] taskContentPattern = null,
            string[] taskContentML = null)
        {
            return GetToDoIntent(
                userInput,
                intents,
                ordinal: ordinal,
                number: number,
                listType: listType,
                containsAll: containsAll,
                taskContentPattern: taskContentPattern,
                taskContentML: taskContentML);
        }
    }
}
