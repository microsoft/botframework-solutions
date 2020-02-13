/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ActivityHandler,
    TurnContext,
    BotState,
    StatePropertyAccessor } from 'botbuilder';
import {
    Dialog,
    DialogState } from 'botbuilder-dialogs';

export class DialogBot<T extends Dialog> extends ActivityHandler {
    private readonly conversationState: BotState;
    private readonly userState: BotState;
    private dialogStateAccesor: StatePropertyAccessor<DialogState>;
    private readonly dialog: Dialog;

    public constructor(
        conversationState: BotState,
        userState: BotState,
        dialog: T
    ) {
        super();

        this.dialog = dialog;
        this.conversationState = conversationState;
        this.userState = userState;
        this.dialogStateAccesor = conversationState.createProperty('DialogState');
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    public async onTurn(turnContext: TurnContext): Promise<any> {

        await this.onTurn(turnContext);

        // Save any state changes that might have occured during the turn.
        await this.conversationState.saveChanges(turnContext, false);
        await this.userState.saveChanges(turnContext, false);
    }

    protected async onMembersAdded(membersAdded: ChannelAccount[], turnContext: TurnContext<ConversationUpdateActivity>){
        return this.dialog.run(turnContext, this.dialogStateAccesor);
    }

    protected async onMessageActivity(turnContext: TurnContext<MessageActivity>){
        return this.dialog.run(turnContext, this.dialogStateAccesor);
    }

    protected async onEventActivity(turnContext :TurnContext<EventActivity>){
        return this.dialog.run(turnContext, this.dialogStateAccesor);
    }
    
}
