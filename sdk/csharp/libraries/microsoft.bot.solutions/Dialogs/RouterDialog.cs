// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;

namespace Microsoft.Bot.Solutions.Dialogs
{
    [Obsolete("Please use ActivityHandlerDialog instead. For more information, refer to https://aka.ms/bfvarouting.", false)]
    [ExcludeFromCodeCoverageAttribute]
    public abstract class RouterDialog : InterruptableDialog
    {
        public RouterDialog(string dialogId, IBotTelemetryClient telemetryClient)
            : base(dialogId, telemetryClient)
        {
            TelemetryClient = telemetryClient;
        }

        protected override Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return OnContinueDialogAsync(innerDc, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var status = await OnInterruptDialogAsync(innerDc, cancellationToken).ConfigureAwait(false);

            if (status == InterruptionAction.Resume)
            {
                // Resume the waiting dialog after interruption
                await innerDc.RepromptDialogAsync().ConfigureAwait(false);
                return EndOfTurn;
            }
            else if (status == InterruptionAction.Waiting)
            {
                // Stack is already waiting for a response, shelve inner stack
                return EndOfTurn;
            }
            else
            {
                var activity = innerDc.Context.Activity;

                if (activity.IsStartActivity())
                {
                    await OnStartAsync(innerDc).ConfigureAwait(false);
                }

                switch (activity.Type)
                {
                    case ActivityTypes.Message:
                        {
                            // Note: This check is a workaround for adaptive card buttons that should map to an event (i.e. startOnboarding button in intro card)
                            if (activity.Value != null)
                            {
                                await OnEventAsync(innerDc).ConfigureAwait(false);
                            }
                            else
                            {
                                var result = await innerDc.ContinueDialogAsync().ConfigureAwait(false);

                                switch (result.Status)
                                {
                                    case DialogTurnStatus.Empty:
                                        {
                                            await RouteAsync(innerDc).ConfigureAwait(false);
                                            break;
                                        }

                                    case DialogTurnStatus.Complete:
                                        {
                                            // End active dialog
                                            await innerDc.EndDialogAsync().ConfigureAwait(false);
                                            break;
                                        }

                                    default:
                                        {
                                            break;
                                        }
                                }
                            }

                            // If the active dialog was ended on this turn (either on single-turn dialog, or on continueDialogAsync) run CompleteAsync method.
                            if (innerDc.ActiveDialog == null)
                            {
                                await CompleteAsync(innerDc).ConfigureAwait(false);
                            }

                            break;
                        }

                    case ActivityTypes.Event:
                        {
                            await OnEventAsync(innerDc).ConfigureAwait(false);
                            break;
                        }

                    case ActivityTypes.Invoke:
                        {
                            // Used by Teams for Authentication scenarios.
                            await innerDc.ContinueDialogAsync().ConfigureAwait(false);
                            break;
                        }

                    default:
                        {
                            await OnSystemMessageAsync(innerDc).ConfigureAwait(false);
                            break;
                        }
                }

                return EndOfTurn;
            }
        }

        protected override Task OnEndDialogAsync(ITurnContext context, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.OnEndDialogAsync(context, instance, reason, cancellationToken);
        }

        protected override Task OnRepromptDialogAsync(ITurnContext turnContext, DialogInstance instance, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.OnRepromptDialogAsync(turnContext, instance, cancellationToken);
        }

        /// <summary>
        /// Called when the inner dialog stack is empty.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected abstract Task RouteAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Called when the inner dialog stack is complete.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="result">The dialog result when inner dialog completed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task CompleteAsync(DialogContext innerDc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            innerDc.EndDialogAsync(result).Wait(cancellationToken);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when an event activity is received.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnEventAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a system activity is received.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnSystemMessageAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a conversation update activity is received.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnStartAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        protected override Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(InterruptionAction.NoAction);
        }
    }
}
