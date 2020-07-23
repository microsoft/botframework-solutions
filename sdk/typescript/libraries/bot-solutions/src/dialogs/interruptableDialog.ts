/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient } from 'botbuilder-core';
import { ComponentDialog, Dialog, DialogContext, DialogTurnResult } from 'botbuilder-dialogs';
import { InterruptionAction } from './interruptionAction';

//OBSOLETE: InterruptableDialog is being deprecated. For more information, refer to https://aka.ms/bfvarouting.
export abstract class InterruptableDialog extends ComponentDialog {
    public primaryDialogName: string;

    public constructor(dialogId: string, telemetryClient: BotTelemetryClient) {
        super(dialogId);
        this.primaryDialogName = dialogId;
        this.telemetryClient = telemetryClient;
    }

    protected async onBeginDialog(dc: DialogContext, options: Object): Promise<DialogTurnResult> {
        if (dc.dialogs.find(this.primaryDialogName) !== undefined) {
            // Overrides default behavior which starts the first dialog added to the stack (i.e. Cancel waterfall)
            return await dc.beginDialog(this.primaryDialogName, options);
        } else {
            // If we don't have a matching dialog, start the initial dialog
            return await dc.beginDialog(this.initialDialogId, options);
        }
    }

    protected async onContinueDialogAsync(dc: DialogContext): Promise<DialogTurnResult> {
        const status: InterruptionAction = await this.onInterruptDialog(dc);

        if (status === InterruptionAction.Resume) {
            // Resume the waiting dialog after interruption
            await dc.repromptDialog();

            return Dialog.EndOfTurn;
        } else if (status === InterruptionAction.Waiting) {
            // Stack is already waiting for a response, shelve inner stack
            return Dialog.EndOfTurn;
        }

        return await super.onContinueDialog(dc);
    }

    protected abstract onInterruptDialog(dc: DialogContext): Promise<InterruptionAction>;
}
