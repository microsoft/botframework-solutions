﻿using Luis;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkillTest.Flow.Utterances
{
    public class DeleteToDoFlowTestUtterances : BaseTestUtterances
    {
        public DeleteToDoFlowTestUtterances()
        {
            this.Add(BaseDeleteTask, GetBaseDeleteToDoIntent(
                BaseDeleteTask));

            this.Add(TaskContent, GetBaseNoneIntent());

            var ordinal = new double[] { 1 };
            this.Add(DeleteSpecificTask, GetBaseDeleteToDoIntent(
                DeleteSpecificTask,
                ordinal: ordinal));

            var listType = new string[] { ToDoStrings.Shopping };
            var number = new double[] { 2 };
            this.Add(DeleteSpecificTaskWithListType, GetBaseDeleteToDoIntent(
                DeleteSpecificTaskWithListType,
                listType: listType,
                number: number));

            number = new double[1];
            var taskContentPattern = new string[] { "play games 1" };
            var taskContentML = new string[] { "play games 1" };
            this.Add(DeleteTaskByContent, GetBaseDeleteToDoIntent(
                DeleteTaskByContent,
                number: number,
                taskContentPattern: taskContentPattern,
                taskContentML: taskContentML));

            var containsAll = new string[] { "all" };
            this.Add(DeleteAllTasks, GetBaseDeleteToDoIntent(
                DeleteAllTasks,
                containsAll: containsAll));
        }

        public static string BaseDeleteTask { get; } = "delete a task";

        public static string TaskContent { get; } = "Play Games 1";

        public static string DeleteSpecificTask { get; } = "delete the first task";

        public static string DeleteSpecificTaskWithListType { get; } = "delete task 2 in my shopping list";

        public static string DeleteTaskByContent { get; } = "delete task Play Games 1";

        public static string DeleteAllTasks { get; } = "remove all my tasks";

        private ToDo GetBaseDeleteToDoIntent(
            string userInput,
            ToDo.Intent intents = ToDo.Intent.DeleteToDo,
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
