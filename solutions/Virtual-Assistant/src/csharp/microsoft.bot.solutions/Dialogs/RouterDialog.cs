using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;

namespace Microsoft.Bot.Solutions.Dialogs
{
    public abstract class RouterDialog : InterruptableDialog
    {
        public RouterDialog(string dialogId, IBotTelemetryClient telemetryClient)
            : base(dialogId, telemetryClient)
        {
            TelemetryClient = telemetryClient;
        }

        protected override Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default(CancellationToken)) => OnContinueDialogAsync(innerDc, cancellationToken);

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var status = await OnInterruptDialogAsync(innerDc, cancellationToken);

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
                    await OnStartAsync(innerDc);
                }

                switch (activity.Type)
                {
                    case ActivityTypes.Message:
                        {
                            var result = await innerDc.ContinueDialogAsync();

                            switch (result.Status)
                            {
                                case DialogTurnStatus.Empty:
                                    {
                                        await RouteAsync(innerDc);
                                        break;
                                    }

                                case DialogTurnStatus.Complete:
                                case DialogTurnStatus.Cancelled:
                                    {
                                        await CompleteAsync(innerDc, result);
                                        break;
                                    }

                                default:
                                    {
                                        break;
                                    }
                            }

                            break;
                        }

                    case ActivityTypes.Event:
                        {
                            await OnEventAsync(innerDc);
                            break;
                        }

                    default:
                        {
                            await OnSystemMessageAsync(innerDc);
                            break;
                        }
                }

                return EndOfTurn;
            }
        }

        protected override Task OnEndDialogAsync(ITurnContext context, DialogInstance instance, DialogReason reason, CancellationToken cancellationToken = default(CancellationToken)) => base.OnEndDialogAsync(context, instance, reason, cancellationToken);

        protected override Task OnRepromptDialogAsync(ITurnContext turnContext, DialogInstance instance, CancellationToken cancellationToken = default(CancellationToken)) => base.OnRepromptDialogAsync(turnContext, instance, cancellationToken);

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
        protected virtual Task OnEventAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken)) => Task.CompletedTask;

        /// <summary>
        /// Called when a system activity is received.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnSystemMessageAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken)) => Task.CompletedTask;

        /// <summary>
        /// Called when a conversation update activity is received.
        /// </summary>
        /// <param name="innerDc">The dialog context for the component.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task OnStartAsync(DialogContext innerDc, CancellationToken cancellationToken = default(CancellationToken)) => Task.CompletedTask;

        protected override Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(InterruptionAction.NoAction);
        }
    }
}