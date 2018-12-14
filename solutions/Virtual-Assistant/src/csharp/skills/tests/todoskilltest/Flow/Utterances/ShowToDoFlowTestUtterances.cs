﻿using Luis;
using ToDoSkill.Dialogs.Shared.Resources;

namespace ToDoSkillTest.Flow.Utterances
{
    public class ShowToDoFlowTestUtterances : BaseTestUtterances
    {
        public ShowToDoFlowTestUtterances()
        {
            var listType = new string[] { ToDoStrings.ToDo };
            this.Add(ShowToDoList, GetBaseShowTasksIntent(ShowToDoList, listType: listType));

            listType = new string[] { ToDoStrings.Grocery };
            this.Add(ShowGroceryList, GetBaseShowTasksIntent(ShowGroceryList, listType: listType));

            listType = new string[] { ToDoStrings.Shopping };
            this.Add(ShowShoppingList, GetBaseShowTasksIntent(ShowShoppingList, listType: listType));
        }

        public static string ShowToDoList { get; } = "Show my to do list";

        public static string ShowGroceryList { get; } = "Show my grocery list";

        public static string ShowShoppingList { get; } = "Show my shopping list";

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
