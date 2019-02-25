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
export class <%= botNameClass %> {
    private readonly botServices: BotServices;
    private readonly conversationState: ConversationState;
    private readonly userState: UserState;
    private readonly dialogs: DialogSet;

    /**
     * Constructs the three pieces necessary for this bot to operate.
     */
    constructor(botServices: BotServices, conversationState: ConversationState, userState: UserState) {
        if (!botServices) { throw new Error(('Missing parameter.  botServices is required')); }
        if (!conversationState) { throw new Error(('Missing parameter.  conversationState is required')); }
        if (!userState) { throw new Error(('Missing parameter.  userState is required')); }

        this.botServices = botServices;
        this.conversationState = conversationState;
        this.userState = userState;

        this.dialogs = new DialogSet(this.conversationState.createProperty<DialogState>('<%= botNameClass %>'));
        this.dialogs.add(new MainDialog(this.botServices, this.conversationState, this.userState));
    }

    /**
     * Run every turn of the conversation. Handles orchestration of messages.
     */
    public async onTurn(turnContext: TurnContext): Promise<void> {
        const dc: DialogContext = await this.dialogs.createContext(turnContext);
        // tslint:disable-next-line:no-any
        const result: DialogTurnResult<any> = await dc.continueDialog();

        if (result.status === DialogTurnStatus.empty) {
            await dc.beginDialog('MainDialog');
        }
    }
}
