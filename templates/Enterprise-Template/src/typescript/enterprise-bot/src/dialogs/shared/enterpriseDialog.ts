// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { RecognizerResult } from 'botbuilder';
import { LuisRecognizer } from 'botbuilder-ai';
import { DialogContext } from 'botbuilder-dialogs';
import { BotServices } from '../../botServices';
import { TelemetryLuisRecognizer } from '../../middleware/telemetry/telemetryLuisRecognizer';
import { CancelDialog } from '../cancel/cancelDialog';
import { CancelResponses } from '../cancel/cancelResponses';
import { MainResponses } from '../main/mainResponses';
import { InterruptableDialog } from './interruptableDialog';
import { InterruptionStatus } from './interruptionStatus';

export class EnterpriseDialog extends InterruptableDialog {

    // Fields
    private readonly services: BotServices;
    private readonly cancelResponder: CancelResponses = new CancelResponses();

    constructor(botServices: BotServices, dialogId: string) {
        super(dialogId);

        this.services = botServices;
        this.addDialog(new CancelDialog());
    }

    protected async onDialogInterruption(dc: DialogContext): Promise<InterruptionStatus> {

        // Check dispatch intent.
        const luisService: TelemetryLuisRecognizer | undefined = this.services.luisServices.get(process.env.LUIS_GENERAL || '');
        if (!luisService) {
            return Promise.reject(
                new Error('The specified LUIS Model could not be found in your Bot Services configuration.')
                );
            } else {
            const luisResult: RecognizerResult = await luisService.recognize(dc.context);
            const intent: string = LuisRecognizer.topIntent(luisResult, undefined, 0.1);

            switch (intent) {
                case 'Cancel':
                return this.onCancel(dc);
                case 'Help':
                return this.onHelp(dc);
                default:
            }
        }

        return InterruptionStatus.NoAction;
    }

    protected async onCancel(dc: DialogContext): Promise<InterruptionStatus> {
        if (dc.activeDialog && dc.activeDialog.id !== CancelDialog.name) {
            // Don't start restart cancel dialog.
            await dc.beginDialog(CancelDialog.name);

            // Signal that the dialog is waiting on user response.
            return InterruptionStatus.Waiting;
        }

        // Else, continue
        return InterruptionStatus.NoAction;
    }

    protected async onHelp(dc: DialogContext): Promise<InterruptionStatus> {
        const view: MainResponses = new MainResponses();
        view.replyWith(dc.context, MainResponses.responseIds.Help);

        // Signal the conversation was interrupted and should immediately continue.
        return InterruptionStatus.Interrupted;
    }
}
