// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ComponentDialog, ConfirmPrompt, DialogContext, DialogTurnResult, WaterfallDialog, WaterfallStepContext } from 'botbuilder-dialogs';
import { CancelResponses } from './cancelResponses';

enum DialogIds {
    cancelPrompt = 'cancelprompt'
}

export class CancelDialog extends ComponentDialog {

    // Fields

    private static readonly responder: CancelResponses = new CancelResponses();

    constructor() {
        super(CancelDialog.name);
        this.initialDialogId = CancelDialog.name;

        const cancel: ((sc: WaterfallStepContext<{}>) => Promise<DialogTurnResult>)[] =  [
            CancelDialog.askToCancel.bind(this),
            CancelDialog.finishCancelDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, cancel));
        this.addDialog(new ConfirmPrompt(DialogIds.cancelPrompt));
    }

    public static async askToCancel (sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.prompt(DialogIds.cancelPrompt, {
            prompt : await CancelDialog.responder.renderTemplate(sc.context,
                                                                 <string> sc.context.activity.locale,
                                                                 CancelResponses.responseIds.CancelPrompt)
        });
    }

    public static async finishCancelDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.endDialog(<boolean> sc.result);
    }

    // tslint:disable-next-line:no-any
    protected async endComponent(outerDC: DialogContext, result: any): Promise<DialogTurnResult> {
        const doCancel: boolean = result;

        if (doCancel) {
            // If user chose to cancel
            await CancelDialog.responder.replyWith(outerDC.context, CancelResponses.responseIds.CancelConfirmedMessage);

            // Cancel all in outer stack of component i.e. the stack the component belongs to
            return outerDC.cancelAllDialogs();
        } else {
            // else if user chose not to cancel
            await CancelDialog.responder.replyWith(outerDC.context, CancelResponses.responseIds.CancelDeniedMessage);

            // End this component. Will trigger reprompt/resume on outer stack
            return outerDC.endDialog();
        }
    }
}
