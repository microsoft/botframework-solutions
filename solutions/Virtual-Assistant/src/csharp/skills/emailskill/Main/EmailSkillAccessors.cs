// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;

    /// <summary>
    /// The email skill accessors.
    /// </summary>
    public class EmailSkillAccessors
    {
        /// <summary>
        /// Gets or sets conversation dialog state.
        /// </summary>
        /// <value>
        /// Conversation dialog state.
        /// </value>
        public IStatePropertyAccessor<DialogState> ConversationDialogState { get; set; }

        /// <summary>
        /// Gets or sets the state used in email bot.
        /// </summary>
        /// <value>
        /// The state used in email bot.
        /// </value>
        public IStatePropertyAccessor<EmailSkillState> EmailSkillState { get; set; }
    }
}
