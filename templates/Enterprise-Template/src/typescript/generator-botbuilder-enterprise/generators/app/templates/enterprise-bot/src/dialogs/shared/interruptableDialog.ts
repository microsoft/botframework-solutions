// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ComponentDialog, Dialog, DialogContext, DialogTurnResult } from 'botbuilder-dialogs';
import { InterruptionStatus } from './interruptionStatus';

export abstract class InterruptableDialog extends ComponentDialog {
    constructor(dialogId: string) { super(dialogId); }

    protected async onContinueDialog(dc: DialogContext): Promise<DialogTurnResult> {
        const status: InterruptionStatus = await this.onDialogInterruption(dc);

        if (status === InterruptionStatus.Interrupted) {
            // Resume the waiting dialog after interruption.
            await dc.repromptDialog();

            return Dialog.EndOfTurn;
        } else if (status === InterruptionStatus.Waiting) {
            // Stack is already waiting for a response, shelve innner stack.
            return Dialog.EndOfTurn;
        }

        return super.onContinueDialog(dc);
    }

    protected abstract onDialogInterruption(dc: DialogContext): Promise<InterruptionStatus>;
}
