// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License

import { BotTelemetryClient } from 'botbuilder';
import {
        DialogTurnResult,
        WaterfallDialog,
        WaterfallStepContext } from 'botbuilder-dialogs';
import { BotServices } from '../../botServices';
import { EnterpriseDialog } from '../shared/enterpriseDialog';
import { EscalateResponses } from './escalateResponses';

export class EscalateDialog extends EnterpriseDialog {
    // Fields
    private static readonly responder: EscalateResponses = new EscalateResponses();

    // Constructor
    constructor(botServices: BotServices, telemetryClient: BotTelemetryClient) {
        super(botServices, EscalateDialog.name, telemetryClient);
        this.initialDialogId = EscalateDialog.name;
        const escalate: ((sc: WaterfallStepContext<{}>) => Promise<DialogTurnResult>)[] = [
            EscalateDialog.sendEscalationMessage.bind(this)
        ];
        this.addDialog(new WaterfallDialog(this.initialDialogId, escalate));
    }

    private static async sendEscalationMessage(sc: WaterfallStepContext): Promise<DialogTurnResult> {
        await EscalateDialog.responder.replyWith(sc.context, EscalateResponses.responseIds.sendEscalationMessage);

        return sc.endDialog();
    }
}
