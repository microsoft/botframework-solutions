// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { ComponentDialog, ConfirmPrompt, DialogContext, DialogTurnResult, WaterfallDialog, WaterfallStepContext } from 'botbuilder-dialogs';
import { CancelResponses } from './cancelResponses';

class DialogIds {
    public CANCEL_PROMPT: string = 'cancelprompt';
}

export class CancelDialog extends ComponentDialog {

    // Fields

    private static readonly RESPONDER: CancelResponses = new CancelResponses();
    private static readonly DIALOG_IDS: DialogIds = new DialogIds();

    constructor() {
        super(CancelDialog.name);
        this.initialDialogId = CancelDialog.name;

        const cancel: ((sc: WaterfallStepContext<{}>) => Promise<DialogTurnResult>)[] =  [
            CancelDialog.ASK_TO_CANCEL.bind(this),
            CancelDialog.FINISH_CANCEL_DIALOG.bind(this)
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, cancel));
        this.addDialog(new ConfirmPrompt(CancelDialog.DIALOG_IDS.CANCEL_PROMPT));
    }

    public static async ASK_TO_CANCEL (sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.prompt(CancelDialog.DIALOG_IDS.CANCEL_PROMPT, {
            prompt : await CancelDialog.RESPONDER.renderTemplate(sc.context,
                                                                 <string> sc.context.activity.locale,
                                                                 CancelResponses.RESPONSE_IDS.CancelPrompt)
        });
    }

    public static async FINISH_CANCEL_DIALOG(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        return sc.endDialog(<boolean> sc.result);
    }

    // tslint:disable-next-line:no-any
    protected async endComponent(outerDC: DialogContext, result: any): Promise<DialogTurnResult> {
        const doCancel: boolean = result;

        if (doCancel) {
            // If user chose to cancel
            await CancelDialog.RESPONDER.replyWith(outerDC.context, CancelResponses.RESPONSE_IDS.CancelConfirmedMessage);

            // Cancel all in outer stack of component i.e. the stack the component belongs to
            return outerDC.cancelAllDialogs();
        } else {
            // else if user chose not to cancel
            await CancelDialog.RESPONDER.replyWith(outerDC.context, CancelResponses.RESPONSE_IDS.CancelDeniedMessage);

            // End this component. Will trigger reprompt/resume on outer stack
            return outerDC.endDialog();
        }
    }
}
