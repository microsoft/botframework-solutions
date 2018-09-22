// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Extensions;

namespace EmailSkill
{
    /// <summary>
    /// To Do help dialog.
    /// </summary>
    public class HelpDialog : ComponentDialog
    {
        /// <summary>
        /// helpDialog Id.
        /// </summary>
        public const string Name = "helpDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpDialog"/> class.
        /// </summary>
        public HelpDialog()
            : base(Name)
        {
            this.AddDialog(new WaterfallDialog(Name, new WaterfallStep[] { this.HelpStep }));
            this.InitialDialogId = Name;
        }

        /// <summary>
        /// Register dialogs.
        /// </summary>
        /// <param name="dialogs">The dialog set.</param>
        public static void Register(DialogSet dialogs)
        {
            dialogs.Add(new HelpDialog());
        }

        /// <summary>
        /// Show help to user.
        /// </summary>
        /// <param name="sc">current step context.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns>Task completion.</returns>
        private async Task<DialogTurnResult> HelpStep(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(EmailBotResponses.EmailHelpMessage));
            return await sc.EndDialogAsync(true);
        }
    }
}
