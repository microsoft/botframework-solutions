// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.Dialogs.ShowToDoTasks
{
    using global::ToDoSkill.Dialogs.AddToDoTask;
    using global::ToDoSkill.Dialogs.Shared;
    using global::ToDoSkill.ServiceClients;
    using Microsoft.Bot.Builder.Dialogs;

    /// <summary>
    /// Show To Do Tasks Container.
    /// </summary>
    public class ShowToDoTasksDialog : ToDoSkillDialog
    {
        /// <summary>
        /// Dialog name.
        /// </summary>
        public const string Name = "showToDoTasksDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowToDoTasksDialog"/> class.
        /// </summary>
        /// <param name="toDoSkillServices">The To Do skill service.</param>
        /// <param name="toDoService">The To Do service.</param>
        /// <param name="accessors">The state accessors.</param>
        public ShowToDoTasksDialog(ToDoSkillServices toDoSkillServices, IToDoService toDoService, ToDoSkillAccessors accessors)
            : base(Name, toDoSkillServices, accessors, toDoService)
        {
            var showToDoTasks = new WaterfallStep[]
            {
                this.GetAuthToken,
                this.AfterGetAuthToken,
                this.ClearContext,
                this.ShowToDoTasks,
                this.AddFirstTask,
            };

            var addFirstTask = new WaterfallStep[]
            {
                this.AskAddFirstTaskConfirmation,
                this.AfterAskAddFirstTaskConfirmation,
            };

            // Define the conversation flow using a waterfall model.
            this.AddDialog(new WaterfallDialog(Action.ShowToDoTasks, showToDoTasks));
            this.AddDialog(new WaterfallDialog(Action.AddFirstTask, addFirstTask));
            this.AddDialog(new AddToDoTaskDialog(toDoSkillServices, toDoService, accessors));

            // Set starting dialog for component
            this.InitialDialogId = Action.ShowToDoTasks;
        }
    }
}
