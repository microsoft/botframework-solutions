// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;

    /// <summary>
    /// To Do state accessors.
    /// </summary>
    public class ToDoSkillAccessors
    {
        /// <summary>
        /// Gets or sets ConversationDialogState.
        /// </summary>
        /// <value>
        /// ConversationDialogState.
        /// </value>
        public IStatePropertyAccessor<DialogState> ConversationDialogState { get; set; }

        /// <summary>
        /// Gets or sets ToDoDialogState.
        /// </summary>
        /// <value>
        /// ToDoDialogState.
        /// </value>
        public IStatePropertyAccessor<ToDoSkillState> ToDoSkillState { get; set; }
    }
}
