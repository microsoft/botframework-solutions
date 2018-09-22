// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;

namespace EmailSkill
{
    /// <summary>
    /// ReplyEmailDialog.
    /// </summary>
    public class ReplyEmailDialog : EmailSkillDialog
    {
        /// <summary>
        /// ReplyEmailDialog Id.
        /// </summary>
        public const string Name = "replyEmailDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplyEmailDialog"/> class.
        /// </summary>
        /// <param name="services">Email skill services.</param>
        /// <param name="accessors">Email skill accessors.</param>
        /// <param name="serviceManager">Email skill service manager.</param>
        public ReplyEmailDialog(EmailSkillServices services, EmailSkillAccessors accessors, IMailSkillServiceManager serviceManager)
            : base(Name, services, accessors, serviceManager)
        {
            var replyEmail = new WaterfallStep[]
            {
                this.GetAuthToken,
                this.AfterGetAuthToken,
                this.CollectSelectedEmail,
                this.CollectAdditionalText,
                this.ConfirmBeforeSending,
                this.ReplyEmail,
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
            this.AddDialog(new WaterfallDialog(Action.Reply, replyEmail));

            // Define the conversation flow using a waterfall model.
            this.AddDialog(new WaterfallDialog(Action.Show, showEmail));
            this.AddDialog(new WaterfallDialog(Action.UpdateSelectMessage, updateSelectMessage));

            this.InitialDialogId = Action.Reply;
        }
    }
}