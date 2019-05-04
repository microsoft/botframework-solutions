/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ActivityHandler,
    BotTelemetryClient,
    ConversationState,
    EndOfConversationCodes,
    Severity,
    TurnContext } from 'botbuilder';
import {
    Dialog,
    DialogContext,
    DialogSet,
    DialogState,
    DialogTurnResult } from 'botbuilder-dialogs';

export class DialogBot<T extends Dialog> extends ActivityHandler {
    private readonly telemetryClient: BotTelemetryClient;
    private readonly solutionName: string = '<%=skillName%>';
    private readonly rootDialogId: string;
    private dialogs: DialogSet;

    constructor(
        conversationState: ConversationState,
        telemetryClient: BotTelemetryClient,
        dialog: T) {
        super();

        this.rootDialogId = dialog.id;
        this.telemetryClient = telemetryClient;
        this.dialogs = new DialogSet(conversationState.createProperty<DialogState>(this.solutionName));
        this.dialogs.add(dialog);
        this.onTurn(this.turn.bind(this));
    }

    //tslint:disable-next-line: no-any
    public async turn(turnContext: TurnContext, next: () => Promise<void>): Promise<any> {
        // Client notifying this bot took to long to respond (timed out)
        if (turnContext.activity.code === EndOfConversationCodes.BotTimedOut) {
            this.telemetryClient.trackTrace({
                message: `Timeout in ${ turnContext.activity.channelId } channel: Bot took too long to respond`,
                severityLevel: Severity.Information
            });

            return;
        }

        const dc: DialogContext = await this.dialogs.createContext(turnContext);

        if (dc.activeDialog !== undefined) {
            const result: DialogTurnResult = await dc.continueDialog();
        } else {
            await dc.beginDialog(this.rootDialogId);
        }

        await next();
    }
}
