// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;

namespace EmailSkill
{
    /// <summary>
    /// ShowEmailDialog.
    /// </summary>
    public class ShowEmailDialog : EmailSkillDialog
    {
        /// <summary>
        /// ShowEmailDialog Id.
        /// </summary>
        public const string Name = "showEmailContainer";

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowEmailDialog"/> class.
        /// </summary>
        /// <param name="services">Email skill services.</param>
        /// <param name="accessors">Email skill accessors.</param>
        /// <param name="serviceManager">Email skill service manager.</param>
        public ShowEmailDialog(EmailSkillServices services, EmailSkillAccessors accessors, IMailSkillServiceManager serviceManager)
            : base(Name, services, accessors, serviceManager)
        {
            var showEmail = new WaterfallStep[]
            {
                this.IfClearContextStep,
                this.GetAuthToken,
                this.AfterGetAuthToken,
                this.ShowEmailsWithOutEnd,
                this.PromptToRead,
                this.CallReadEmailDialog,
            };

            var readEmail = new WaterfallStep[]
            {
                this.ReadEmail,
                this.AfterReadOutEmail,
            };

            // Define the conversation flow using a waterfall model.
            this.AddDialog(new WaterfallDialog(Action.Show, showEmail));
            this.AddDialog(new WaterfallDialog(Action.Read, readEmail));
            this.InitialDialogId = Action.Show;
        }
    }
}
