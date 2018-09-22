// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.Dialogs.DeleteToDoTask
{
    using global::ToDoSkill.Dialogs.Shared;
    using global::ToDoSkill.ServiceClients;
    using Microsoft.Bot.Builder.Dialogs;

    /// <summary>
    /// Delete To Do Tasks Container.
    /// </summary>
    public class DeleteToDoTaskDialog : ToDoSkillDialog
    {
        /// <summary>
        /// Dialog name.
        /// </summary>
        public const string Name = "deleteToDoTaskDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteToDoTaskDialog"/> class.
        /// </summary>
        /// <param name="toDoSkillServices">The To Do skill service.</param>
        /// <param name="toDoService">The To Do service.</param>
        /// <param name="accessors">The state accessors.</param>
        public DeleteToDoTaskDialog(ToDoSkillServices toDoSkillServices, IToDoService toDoService, ToDoSkillAccessors accessors)
            : base(Name, toDoSkillServices, accessors, toDoService)
        {
            var deleteToDoTask = new WaterfallStep[]
            {
                this.GetAuthToken,
                this.AfterGetAuthToken,
                this.ClearContext,
                this.CollectToDoTaskIndex,
                this.CollectAskDeletionConfirmation,
                this.DeleteToDoTask,
            };

            var collectToDoTaskIndex = new WaterfallStep[]
            {
                this.AskToDoTaskIndex,
                this.AfterAskToDoTaskIndex,
            };

            var collectDeleteTaskConfirmation = new WaterfallStep[]
            {
                this.AskDeletionConfirmation,
                this.AfterAskDeletionConfirmation,
            };

            // Define the conversation flow using a waterfall model.
            this.AddDialog(new WaterfallDialog(Action.DeleteToDoTask, deleteToDoTask));
            this.AddDialog(new WaterfallDialog(Action.CollectToDoTaskIndex, collectToDoTaskIndex));
            this.AddDialog(new WaterfallDialog(Action.CollectDeleteTaskConfirmation, collectDeleteTaskConfirmation));

            // Set starting dialog for component
            this.InitialDialogId = Action.DeleteToDoTask;
        }
    }
}
