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
    DialogState } from 'botbuilder-dialogs';

export class DialogBot<T extends Dialog> extends ActivityHandler {
    private readonly telemetryClient: BotTelemetryClient;
    private readonly solutionName: string = 'sampleSkill';
    private readonly rootDialogId: string;
    private readonly dialogs: DialogSet;

    public constructor(
        conversationState: ConversationState,
        telemetryClient: BotTelemetryClient,
        dialog: T
    ) {
        super();

        this.rootDialogId = dialog.id;
        this.telemetryClient = telemetryClient;
        this.dialogs = new DialogSet(conversationState.createProperty<DialogState>(this.solutionName));
        this.dialogs.add(dialog);
        this.onTurn(this.turn.bind(this));
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/tslint/config
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
            await dc.continueDialog();
        } else {
            await dc.beginDialog(this.rootDialogId);
        }

        await next();
    }
}
