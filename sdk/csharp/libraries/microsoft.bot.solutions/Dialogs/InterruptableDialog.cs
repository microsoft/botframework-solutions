﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace Microsoft.Bot.Solutions.Dialogs
{
    [Obsolete("InterruptableDialog is being deprecated. For more information, refer to https://aka.ms/bfvarouting.", false)]
    public abstract class InterruptableDialog : ComponentDialog
    {
        public InterruptableDialog(string dialogId, IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            PrimaryDialogName = dialogId;
            TelemetryClient = telemetryClient;
        }

        public string PrimaryDialogName { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc.Dialogs.Find(PrimaryDialogName) != null)
            {
                // Overrides default behavior which starts the first dialog added to the stack (i.e. Cancel waterfall)
                return await dc.BeginDialogAsync(PrimaryDialogName, options).ConfigureAwait(false);
            }
            else
            {
                // If we don't have a matching dialog, start the initial dialog
                return await dc.BeginDialogAsync(InitialDialogId, options).ConfigureAwait(false);
            }
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            var status = await OnInterruptDialogAsync(dc, cancellationToken).ConfigureAwait(false);

            if (status == InterruptionAction.Resume)
            {
                // Resume the waiting dialog after interruption
                await dc.RepromptDialogAsync().ConfigureAwait(false);
                return EndOfTurn;
            }
            else if (status == InterruptionAction.Waiting)
            {
                // Stack is already waiting for a response, shelve inner stack
                return EndOfTurn;
            }

            return await base.OnContinueDialogAsync(dc, cancellationToken).ConfigureAwait(false);
        }

        protected abstract Task<InterruptionAction> OnInterruptDialogAsync(DialogContext dc, CancellationToken cancellationToken);
    }
}