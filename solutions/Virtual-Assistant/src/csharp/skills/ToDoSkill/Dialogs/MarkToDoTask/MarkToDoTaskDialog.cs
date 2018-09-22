// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkillLibrary.Dialogs.MarkToDoTask
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Solutions.Extensions;
    using ToDoSkill;
    using ToDoSkill.Dialogs.Shared;
    using ToDoSkill.ServiceClients;

    /// <summary>
    /// Mark To Do Tasks Container.
    /// </summary>
    public class MarkToDoTaskDialog : ToDoSkillDialog
    {
        /// <summary>
        /// Dialog name.
        /// </summary>
        public const string Name = "markToDoTaskDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkToDoTaskDialog"/> class.
        /// </summary>
        /// <param name="toDoSkillServices">The To Do skill service.</param>
        /// <param name="toDoService">The To Do service.</param>
        /// <param name="accessors">The state accessors.</param>
        public MarkToDoTaskDialog(ToDoSkillServices toDoSkillServices, IToDoService toDoService, ToDoSkillAccessors accessors)
            : base(Name, toDoSkillServices, accessors, toDoService)
        {
            var markToDoTask = new WaterfallStep[]
            {
                this.GetAuthToken,
                this.AfterGetAuthToken,
                this.ClearContext,
                this.CollectToDoTaskIndex,
                this.MarkToDoTaskCompleted,
            };

            var collectToDoTaskIndex = new WaterfallStep[]
            {
                this.AskToDoTaskIndex,
                this.AfterAskToDoTaskIndex,
            };

            // Define the conversation flow using a waterfall model.
            this.AddDialog(new WaterfallDialog(Action.MarkToDoTaskCompleted, markToDoTask));
            this.AddDialog(new WaterfallDialog(Action.CollectToDoTaskIndex, collectToDoTaskIndex));

            // Set starting dialog for component
            this.InitialDialogId = Action.MarkToDoTaskCompleted;
        }
    }
}
