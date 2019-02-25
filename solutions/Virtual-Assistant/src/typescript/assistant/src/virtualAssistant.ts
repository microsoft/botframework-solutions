// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import {
    ConversationState,
    TurnContext,
    UserState } from 'botbuilder';
import {
    DialogContext,
    DialogSet,
    DialogState,
    DialogTurnResult,
    DialogTurnStatus } from 'botbuilder-dialogs';
import { BotServices } from './botServices';
import { MainDialog } from './dialogs/main/mainDialog';

/**
 * Main entry point and orchestration for bot.
 */
export class VirtualAssistant {
    private readonly BOT_SERVICES: BotServices;
    private readonly CONVERSATION_STATE: ConversationState;
    private readonly USER_STATE: UserState;
    private readonly DIALOGS: DialogSet;

    /**
     * Constructs the three pieces necessary for this bot to operate.
     */
    constructor(botServices: BotServices, conversationState: ConversationState, userState: UserState) {
        if (!botServices) { throw new Error(('Missing parameter.  botServices is required')); }
        if (!conversationState) { throw new Error(('Missing parameter.  conversationState is required')); }
        if (!userState) { throw new Error(('Missing parameter.  userState is required')); }

        this.BOT_SERVICES = botServices;
        this.CONVERSATION_STATE = conversationState;
        this.USER_STATE = userState;

        this.DIALOGS = new DialogSet(this.CONVERSATION_STATE.createProperty<DialogState>('VirtualAssistant'));
        this.DIALOGS.add(new MainDialog());
    }

    /**
     * Run every turn of the conversation. Handles orchestration of messages.
     */
    public async onTurn(turnContext: TurnContext): Promise<void> {
        const dc: DialogContext = await this.DIALOGS.createContext(turnContext);
        // tslint:disable-next-line:no-any
        const result: DialogTurnResult<any> = await dc.continueDialog();

        if (result.status === DialogTurnStatus.empty) {
            await dc.beginDialog('MainDialog');
        }
    }
}
