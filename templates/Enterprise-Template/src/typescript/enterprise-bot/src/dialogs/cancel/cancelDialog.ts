// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ComponentDialog, ConfirmPrompt, DialogContext, DialogTurnResult, WaterfallDialog, WaterfallStepContext } from 'botbuilder-dialogs';
import { CancelResponses } from './cancelResponses';

export class CancelDialog extends ComponentDialog {

    // Fields
    private static readonly _responder: CancelResponses = new CancelResponses();

    constructor() {
        super(CancelDialog.name);
        this.initialDialogId = CancelDialog.name;

        const cancel = [
            CancelDialog.AskToCancel.bind(this),
            CancelDialog.FinishCancelDialog.bind(this)
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, cancel));
        this.addDialog(new ConfirmPrompt(DialogIds.CancelPrompt));
    }

    public static async AskToCancel(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.prompt(DialogIds.CancelPrompt, {
            prompt : await CancelDialog._responder.renderTemplate(sc.context, sc.context.activity.locale as string , CancelResponses.ResponseIds.CancelPrompt)
        });
    }

    public static FinishCancelDialog(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.endDialog(sc.result as boolean);
    }

    protected async endComponent(outerDC: DialogContext, result: any) {
        const doCancel: boolean = result;

        if (doCancel) {
            // If user chose to cancel
            await CancelDialog._responder.replyWith(outerDC.context, CancelResponses.ResponseIds.CancelConfirmedMessage);

            // Cancel all in outer stack of component i.e. the stack the component belongs to
            return outerDC.cancelAllDialogs();
        } else {
            // else if user chose not to cancel
            await CancelDialog._responder.replyWith(outerDC.context, CancelResponses.ResponseIds.CancelDeniedMessage);

            // End this component. Will trigger reprompt/resume on outer stack
            return outerDC.endDialog();
        }
    }
}

class DialogIds {
    public static CancelPrompt: string = 'cancelPrompt';
}
