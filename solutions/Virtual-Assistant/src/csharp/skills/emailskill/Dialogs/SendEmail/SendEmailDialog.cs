// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;

namespace EmailSkill
{
    /// <summary>
    /// SendEmailDialog.
    /// </summary>
    public class SendEmailDialog : EmailSkillDialog
    {
        /// <summary>
        /// SendEmailDialog Id.
        /// </summary>
        public const string Name = "sendEmailDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="SendEmailDialog"/> class.
        /// </summary>
        /// <param name="services">Email skill services.</param>
        /// <param name="accessors">Email skill accessors.</param>
        /// <param name="serviceManager">Email skill service manager.</param>
        public SendEmailDialog(EmailSkillServices services, EmailSkillAccessors accessors, IMailSkillServiceManager serviceManager)
            : base(Name, services, accessors, serviceManager)
        {
            var sendEmail = new WaterfallStep[]
            {
                this.GetAuthToken,
                this.AfterGetAuthToken,
                this.CollectNameList,
                this.CollectRecipients,
                this.CollectSubject,
                this.CollectText,
                this.ConfirmBeforeSending,
                this.SendEmail,
            };

            // Define the conversation flow using a waterfall model.
            this.AddDialog(new WaterfallDialog(Action.Send, sendEmail));
            this.AddDialog(new ConfirmRecipientDialog(services, accessors, serviceManager));
            this.InitialDialogId = Action.Send;
        }
    }
}
