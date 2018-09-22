// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.Dialogs.AddToDoTask
{
    using global::ToDoSkill.Dialogs.Shared;
    using global::ToDoSkill.ServiceClients;
    using Microsoft.Bot.Builder.Dialogs;

    /// <summary>
    /// Add To Do Task Container.
    /// </summary>
    public class AddToDoTaskDialog : ToDoSkillDialog
    {
        /// <summary>
        /// Dialog name.
        /// </summary>
        public const string Name = "addToDoTasksDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="AddToDoTaskDialog"/> class.
        /// </summary>
        /// <param name="toDoSkillServices">The To Do skill service.</param>
        /// <param name="toDoService">The To Do service.</param>
        /// <param name="accessors">The state accessors.</param>
        public AddToDoTaskDialog(ToDoSkillServices toDoSkillServices, IToDoService toDoService, ToDoSkillAccessors accessors)
            : base(Name, toDoSkillServices, accessors, toDoService)
        {
            var addToDoTask = new WaterfallStep[]
            {
                this.GetAuthToken,
                this.AfterGetAuthToken,
                this.ClearContext,
                this.CollectToDoTaskContent,
                this.AddToDoTask,
            };

            var collectToDoTaskContent = new WaterfallStep[]
            {
                this.AskToDoTaskContent,
                this.AfterAskToDoTaskContent,
            };

            // Define the conversation flow using a waterfall model.
            this.AddDialog(new WaterfallDialog(Action.AddToDoTask, addToDoTask));
            this.AddDialog(new WaterfallDialog(Action.CollectToDoTaskContent, collectToDoTaskContent));

            // Set starting dialog for component
            this.InitialDialogId = Action.AddToDoTask;
        }
    }
}
