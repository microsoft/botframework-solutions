// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import {
    DialogTurnResult,
    WaterfallDialog,
    WaterfallStepContext } from 'botbuilder-dialogs';
import { BotServices } from '../../botServices';
import { EnterpriseDialog } from '../shared/enterpriseDialog';
import { EscalateResponses } from './escalateResponses';

export class EscalateDialog extends EnterpriseDialog {

    // Fields
    public static readonly responder: EscalateResponses = new EscalateResponses();

    constructor(botServices: BotServices) {
        super(botServices, EscalateDialog.name);
        this.initialDialogId = EscalateDialog.name;

        // tslint:disable-next-line:no-any
        const escalate: ((sc: WaterfallStepContext<{}>) => Promise<DialogTurnResult<any>>)[] = [
            EscalateDialog.sendPhone.bind(this)
        ];

        this.addDialog(new WaterfallDialog(this.initialDialogId, escalate));
    }

    private static async sendPhone(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        await EscalateDialog.responder.replyWith(sc.context, EscalateResponses.responseIds.SendPhoneMessage);

        return sc.endDialog();
    }
}
