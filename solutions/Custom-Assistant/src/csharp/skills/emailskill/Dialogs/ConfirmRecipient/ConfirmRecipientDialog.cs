// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;

namespace EmailSkill
{
    /// <summary>
    /// Dialog to confirm hte recipient.
    /// </summary>
    public class ConfirmRecipientDialog : EmailSkillDialog
    {
        /// <summary>
        /// Confirm recipient dialog Id.
        /// </summary>
        public const string Name = "confirmRecipientContainer";

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmRecipientDialog"/> class.
        /// </summary>
        /// <param name="services">Email skill services.</param>
        /// <param name="accessors">Email skill accessors.</param>
        /// <param name="serviceManager">Email skill service manager.</param>
        public ConfirmRecipientDialog(EmailSkillServices services, EmailSkillAccessors accessors, IMailSkillServiceManager serviceManager)
            : base(Name, services, accessors, serviceManager)
        {
            var confirmRecipient = new WaterfallStep[]
            {
                this.ConfirmRecipient,
                this.AfterConfirmRecipient,
            };

            var updateRecipientName = new WaterfallStep[]
            {
                this.UpdateUserName,
                this.AfterUpdateUserName,
            };

            // Define the conversation flow using a waterfall model.
            this.AddDialog(new WaterfallDialog(Action.ConfirmRecipient, confirmRecipient));
            this.AddDialog(new WaterfallDialog(Action.UpdateRecipientName, updateRecipientName));
            this.InitialDialogId = Action.ConfirmRecipient;
        }
    }
}
