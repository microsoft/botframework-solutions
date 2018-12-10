using Luis;

namespace ToDoSkillTest.Flow.Utterances
{
    public class AddToDoFlowTestUtterances : BaseTestUtterances
    {
        public AddToDoFlowTestUtterances()
        {
            this.Add(BaseAddTask, GetBaseShowTasksIntent(BaseAddTask));
            this.Add(TaskContent, GetBaseNoneIntent());
        }

        public static string BaseAddTask { get; } = "add a task";

        public static string TaskContent { get; } = "call my mother";

        private ToDo GetBaseShowTasksIntent(
            string userInput,
            ToDo.Intent intents = ToDo.Intent.AddToDo,
            string[] listType = null)
        {
            return GetToDoIntent(
                userInput,
                intents,
                listType: listType);
        }
    }
}
