/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { IBackgroundTaskQueue, ProactiveState } from 'botbuilder-solutions';
import { BotTelemetryClient, ConversationState, EndOfConversationCodes, TurnContext, UserState } from 'botbuilder';
import { DialogContext, DialogSet, DialogState, DialogTurnResult } from 'botbuilder-dialogs';
import { IEndpointService } from 'botframework-config';
import { BotServices } from './botServices';
import { MainDialog } from './dialogs/main/mainDialog';

/**
 * Main entry point and orchestration for bot.
 */
export class VirtualAssistant {
    private readonly services: BotServices;
    private readonly conversationState: ConversationState;
    private readonly userState: UserState;
    private readonly proactiveState: ProactiveState;
    private readonly endpointService: IEndpointService;
    private readonly telemetryClient: BotTelemetryClient;
    private readonly backgroundTaskQueue: IBackgroundTaskQueue;
    private readonly dialogs: DialogSet;

    /**
     * Initializezs a new instance of the VirtualAssistant class.
     * botServices - Bot services.
     * conversationState - Bot conversation state.
     * userState - Bot user state.
     * proactiveState - Proactive state.
     * endpointService - Bot endpoint service.
     * telemetryClient - Bot telemetry client.
     * backgroundTaskQueue - Background task queu.
     */
    constructor(botServices: BotServices,
                conversationState: ConversationState,
                userState: UserState,
                proactiveState: ProactiveState,
                endpointService: IEndpointService,
                telemetryClient: BotTelemetryClient,
                backgroundTaskQueue: IBackgroundTaskQueue) {
        if (!conversationState) { throw new Error('Missing parameter.  conversationState is required'); }
        if (!userState) { throw new Error('Missing parameter.  userState is required'); }
        if (!proactiveState) { throw new Error('Missing parameter. proactiveState is required'); }
        if (!botServices) { throw new Error('Missing parameter.  botServices is required'); }
        if (!endpointService) { throw new Error('Missing parameter.  endpointService is required'); }
        if (!telemetryClient) { throw new Error('Missing parameter.  telemetryClient is required'); }

        this.services = botServices;
        this.conversationState = conversationState;
        this.userState = userState;
        this.proactiveState = proactiveState;
        this.endpointService = endpointService;
        this.telemetryClient = telemetryClient;
        this.backgroundTaskQueue = backgroundTaskQueue;

        this.dialogs = new DialogSet(this.conversationState.createProperty<DialogState>(VirtualAssistant.name));
        this.dialogs.add(new MainDialog(
            this.services,
            this.conversationState,
            this.userState,
            this.proactiveState,
            this.endpointService,
            this.telemetryClient,
            this.backgroundTaskQueue));
    }

    /**
     * Run every turn of the conversation. Handles orchestration of messages.
     */
    public async onTurn(turnContext: TurnContext): Promise<void> {
        // Client notifying this bot took to long to respond (timed out)
        if (turnContext.activity.code === EndOfConversationCodes.BotTimedOut) {
            const message: string = `Timeout in ${turnContext.activity.channelId} channel: Bot took too long to respond.`;
            this.services.telemetryClient.trackTrace({ message: message });

            return;
        }

        const dc: DialogContext = await this.dialogs.createContext(turnContext);

        if (!!dc.activeDialog) {
            const result: DialogTurnResult = await dc.continueDialog();
        } else {
            await dc.beginDialog(MainDialog.name);
        }
    }
}
