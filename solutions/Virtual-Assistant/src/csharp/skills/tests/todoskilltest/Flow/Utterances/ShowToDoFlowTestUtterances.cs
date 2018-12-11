using Luis;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkillTest.Flow.Utterances
{
    public class ShowToDoFlowTestUtterances : BaseTestUtterances
    {
        public ShowToDoFlowTestUtterances()
        {
            var listType = new string[] { ToDoStrings.ToDo };
            this.Add(BaseShowTasks, GetBaseShowTasksIntent(BaseShowTasks, listType: listType));
        }

        public static string BaseShowTasks { get; } = "Show my to do list";

        private ToDo GetBaseShowTasksIntent(
            string userInput,
            ToDo.Intent intents = ToDo.Intent.ShowToDo,
            string[] listType = null)
        {
            return GetToDoIntent(
                userInput,
                intents,
                listType: listType);
        }
    }
}
