// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Dialogs
{
    /// <summary>
    /// Provides interruption logic and methods for handling incoming activities based on type.
    /// </summary>
    [Obsolete("ActivityHandlerDialog is being deprecated. For more information, refer to https://aka.ms/bfvarouting.", false)]
    [ExcludeFromCodeCoverageAttribute]
    public abstract class ActivityHandlerDialog : InterruptableDialog
    {
        public ActivityHandlerDialog(
            string dialogId,
            IBotTelemetryClient telemetryClient)
            : base(dialogId, telemetryClient)
        {
            TelemetryClient = telemetryClient;
        }

        /// <summary>
        /// Called when the dialog is started and pushed onto the parent's dialog stack.
        /// </summary>
        /// <param name="innerDc">The inner <see cref="DialogContext"/> for the current turn of conversation.</param>
        /// <param name="options">Optional, initial information to pass to the dialog.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>If the task is successful, the result indicates whether the dialog is still
        /// active after the turn has been processed by the dialog. The result may also contain a
        /// return value.
        /// </remarks>
        protected override Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            return OnContinueDialogAsync(innerDc, cancellationToken);
        }

        /// <summary>
        /// Called when the dialog is continued, where it is the active dialog and the
        /// user replies with a new activity.
        /// </summary>
        /// <param name="innerDc">The inner <see cref="DialogContext"/> for the current turn of conversation.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>If the task is successful, the result indicates whether the dialog is still
        /// active after the turn has been processed by the dialog. The result may also contain a
        /// return value.
        ///
        /// By default, this calls <see cref="InterruptableDialog.OnInterruptDialogAsync(DialogContext, CancellationToken)"/>
        /// then routes the activity to the waiting active dialog, or to a handling method based on its activity type.
        /// </remarks>
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            // Check for any interruptions.
            var status = await OnInterruptDialogAsync(innerDc, cancellationToken).ConfigureAwait(false);

            if (status == InterruptionAction.Resume)
            {
                // Interruption message was sent, and the waiting dialog should resume/reprompt.
                await innerDc.RepromptDialogAsync().ConfigureAwait(false);
            }
            else if (status == InterruptionAction.Waiting)
            {
                // Interruption intercepted conversation and is waiting for user to respond.
                return EndOfTurn;
            }
            else if (status == InterruptionAction.End)
            {
                // Interruption ended conversation, and current dialog should end.
                return await innerDc.EndDialogAsync().ConfigureAwait(false);
            }
            else if (status == InterruptionAction.NoAction)
            {
                // No interruption was detected. Process activity normally.
                var activity = innerDc.Context.Activity;

                switch (activity.Type)
                {
                    case ActivityTypes.Message:
                        {
                            // Pass message to waiting child dialog.
                            var result = await innerDc.ContinueDialogAsync().ConfigureAwait(false);

                            if (result.Status == DialogTurnStatus.Empty)
                            {
                                // There was no waiting dialog on the stack, process message normally.
                                await OnMessageActivityAsync(innerDc).ConfigureAwait(false);
                            }

                            break;
                        }

                    case ActivityTypes.Event:
                        {
                            await OnEventActivityAsync(innerDc).ConfigureAwait(false);
                            break;
                        }

                    case ActivityTypes.Invoke:
                        {
                            // Used by Teams for Authentication scenarios.
                            await innerDc.ContinueDialogAsync().ConfigureAwait(false);
                            break;
                        }

                    case ActivityTypes.ConversationUpdate:
                        {
                            await OnMembersAddedAsync(innerDc).ConfigureAwait(false);
                            break;
                        }

                    default:
                        {
                            // All other activity types will be routed here. Custom handling should be added in implementation.
                            await OnUnhandledActivityTypeAsync(innerDc).ConfigureAwait(false);
                            break;
                        }
                }
            }

            if (innerDc.ActiveDialog == null)
            {
                // If the inner dialog stack completed during this turn, this component should be ended.
                return await innerDc.EndDialogAsync().ConfigureAwait(false);
            }

            return EndOfTurn;
        }

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            // This happens when an inner dialog ends. Could call complete here
            await OnDialogCompleteAsync(outerDc, result, cancellationToken).ConfigureAwait(false);
            return await base.EndComponentAsync(outerDc, result, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Called on every turn, enabling interruption scenarios.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> returning an <see cref="InterruptionAction">
        /// which indicates what action should be taken after interruption.</returns>.
        protected override Task<InterruptionAction> OnInterruptDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            return Task.FromResult(InterruptionAction.NoAction);
        }

        /// <summary>
        /// Called when an event activity is received.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnEventActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a message activity is received.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnMessageActivityAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a conversationUpdate activity is received.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnMembersAddedAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when an activity type other than event, message, or conversationUpdate is received.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnUnhandledActivityTypeAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when the inner dialog stack completes.
        /// </summary>
        /// <param name="outerDc">The dialog context for the component.</param>
        /// <param name="result">The dialog turn result for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnDialogCompleteAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
