// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;

namespace EmailSkill
{
    /// <summary>
    /// ForwardEmailDialog.
    /// </summary>
    public class ForwardEmailDialog : EmailSkillDialog
    {
        /// <summary>
        /// Forward email dialog Id.
        /// </summary>
        public const string Name = "forwardEmailContainer";

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardEmailDialog"/> class.
        /// </summary>
        /// <param name="services">Email skill services.</param>
        /// <param name="accessors">Email skill accessors.</param>
        /// <param name="serviceManager">Email skill service manager.</param>
        public ForwardEmailDialog(EmailSkillServices services, EmailSkillAccessors accessors, IMailSkillServiceManager serviceManager)
            : base(Name, services, accessors, serviceManager)
        {
            var forwardEmail = new WaterfallStep[]
            {
                this.GetAuthToken,
                this.AfterGetAuthToken,
                this.CollectNameList,
                this.CollectRecipients,
                this.CollectSelectedEmail,
                this.CollectAdditionalText,
                this.ConfirmBeforeSending,
                this.ForwardEmail,
            };

            var showEmail = new WaterfallStep[]
            {
                this.ShowEmails,
            };

            var updateSelectMessage = new WaterfallStep[]
            {
                this.UpdateMessage,
                this.PromptUpdateMessage,
                this.AfterUpdateMessage,
            };

            // Define the conversation flow using a waterfall model.
            this.AddDialog(new WaterfallDialog(Action.Forward, forwardEmail));
            this.AddDialog(new WaterfallDialog(Action.Show, showEmail));
            this.AddDialog(new WaterfallDialog(Action.UpdateSelectMessage, updateSelectMessage));
            this.AddDialog(new ConfirmRecipientDialog(services, accessors, serviceManager));
            this.InitialDialogId = Action.Forward;
        }
    }
}
