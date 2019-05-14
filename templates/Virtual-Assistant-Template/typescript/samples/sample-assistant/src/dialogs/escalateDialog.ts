/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { BotTelemetryClient } from 'botbuilder';
import {
    ComponentDialog,
    DialogTurnResult,
    WaterfallDialog,
    WaterfallStepContext } from 'botbuilder-dialogs';
import { EscalateResponses } from '../responses/escalateResponses';
import { BotServices } from '../services/botServices';

export class EscalateDialog extends ComponentDialog {
    // Fields
    private readonly responder: EscalateResponses = new EscalateResponses();

    // Constructor
    constructor(botServices: BotServices, telemetryClient: BotTelemetryClient) {
        super(EscalateDialog.name);
        this.initialDialogId = EscalateDialog.name;
        const escalate: ((sc: WaterfallStepContext) => Promise<DialogTurnResult>)[] = [
            this.sendPhone.bind(this)
        ];
        this.addDialog(new WaterfallDialog(
            this.initialDialogId,
            escalate
        ));
    }

    private async sendPhone(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        await this.responder.replyWith(sc.context, EscalateResponses.responseIds.sendPhoneMessage);

        return sc.endDialog();
    }
}
