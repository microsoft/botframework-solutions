/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ComponentDialog,
    ConfirmPrompt,
    DialogContext,
    DialogTurnResult,
    WaterfallDialog,
    WaterfallStepContext } from 'botbuilder-dialogs';
import { CancelResponses } from '../responses/cancelResponses';

enum DialogIds {
    cancelPrompt = 'cancelprompt'
}

export class CancelDialog extends ComponentDialog {

    // Fields
    private readonly responder: CancelResponses = new CancelResponses();

    // Constructor
    constructor() {
        super(CancelDialog.name);
        this.initialDialogId = CancelDialog.name;

        const cancel: ((sc: WaterfallStepContext) => Promise<DialogTurnResult>)[] =  [
            this.askToCancel.bind(this),
            this.finishCancelDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, cancel));
        this.addDialog(new ConfirmPrompt(DialogIds.cancelPrompt));
    }

    protected async endComponent(outerDC: DialogContext, result: boolean): Promise<DialogTurnResult> {
        const doCancel: boolean = result;

        if (doCancel) {
            // If user chose to cancel
            await this.responder.replyWith(outerDC.context, CancelResponses.responseIds.cancelConfirmedMessage);

            // Cancel all in outer stack of component i.e. the stack the component belongs to
            return outerDC.cancelAllDialogs();
        } else {
            // else if user chose not to cancel
            await this.responder.replyWith(outerDC.context, CancelResponses.responseIds.cancelDeniedMessage);

            // End this component. Will trigger reprompt/resume on outer stack
            return outerDC.endDialog();
        }
    }

    private async askToCancel (sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.prompt(DialogIds.cancelPrompt, {
            prompt: await this.responder.renderTemplate
            (
                sc.context,
                <string> sc.context.activity.locale,
                CancelResponses.responseIds.cancelPrompt
            )
        });
    }

    private async finishCancelDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.endDialog(<boolean> sc.result);
    }
}
