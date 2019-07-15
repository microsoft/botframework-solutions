using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Dialogs;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills.Dialogs
{
    public class DispatchDialog : RouterDialog
    {
        public DispatchDialog(string dialogId, IBotTelemetryClient telemetryClient)
            : base(dialogId, telemetryClient)
        {
        }

        protected override Task RouteAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        // Add support to skill level fallback and redispatch
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var status = await OnInterruptDialogAsync(innerDc, cancellationToken).ConfigureAwait(false);

            if (status == InterruptionAction.MessageSentToUser)
            {
                // Resume the waiting dialog after interruption
                await innerDc.RepromptDialogAsync().ConfigureAwait(false);
                return EndOfTurn;
            }
            else if (status == InterruptionAction.StartedDialog)
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
                            else if (!string.IsNullOrEmpty(activity.Text))
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
                                            if (result.Result is DispatchIntent intent)
                                            {
                                                // Redispatch when a intent is matched to a new skill
                                                await RedispatchAsync(innerDc, result).ConfigureAwait(false);
                                            }
                                            else
                                            {
                                                await CompleteAsync(innerDc).ConfigureAwait(false);

                                                // End active dialog
                                                await innerDc.EndDialogAsync().ConfigureAwait(false);
                                            }

                                            break;
                                        }

                                    default:
                                        {
                                            break;
                                        }
                                }
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

        /// <summary>
        /// Called when fallbackhandler event is recieved.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="result">The dialog result when inner dialog completed.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task RedispatchAsync(DialogContext innerDc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}
