// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.Dialogs.Greeting
{
    using global::ToDoSkill.Dialogs.Shared;
    using global::ToDoSkill.ServiceClients;
    using Microsoft.Bot.Builder.Dialogs;

    /// <summary>
    /// GreetingDialog.
    /// </summary>
    public class GreetingDialog : ToDoSkillDialog
    {
        /// <summary>
        /// GreetingDialog Id.
        /// </summary>
        public const string Name = "greetingDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="GreetingDialog"/> class.
        /// </summary>
        /// <param name="toDoSkillServices">The To Do skill service.</param>
        /// <param name="toDoService">The To Do service.</param>
        /// <param name="accessors">The state accessors.</param>
        public GreetingDialog(ToDoSkillServices toDoSkillServices, IToDoService toDoService, ToDoSkillAccessors accessors)
            : base(Name, toDoSkillServices, accessors, toDoService)
        {
            // Define the conversation flow using a waterfall model.
            this.AddDialog(new WaterfallDialog(Action.Greeting, new WaterfallStep[] { this.GreetingStep }));

            // Set starting dialog for component
            this.InitialDialogId = Action.Greeting;
        }
    }
}
