/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import { TeamsActivityHandler, ConversationState, UserState, StatePropertyAccessor, TurnContext } from 'botbuilder';
import { Dialog, DialogState } from 'botbuilder-dialogs';
import { DialogEx } from 'botbuilder-solutions';

export class DefaultActivityHandler<T extends Dialog> extends TeamsActivityHandler {
    private readonly dialog: Dialog;
    private readonly conversationState: ConversationState;
    private readonly userState: UserState;
    private dialogStateAccessor: StatePropertyAccessor;

    public constructor (        
        conversationState: ConversationState,
        userState: UserState,
        dialog: T) {
        super();
        
        this.dialog = dialog;
        this.conversationState = conversationState;
        this.userState = userState;
        this.dialogStateAccessor = conversationState.createProperty<DialogState>('DialogState');
    }

    public async turn(turnContext: TurnContext): Promise<any> {
        await super.onTurnActivity(turnContext);

        // Save any state changes that might have occured during the turn.
        await this.conversationState.saveChanges(turnContext, false);
        await this.userState.saveChanges(turnContext, false);
    }

    protected async onTeamsMembersAdded(turnContext: TurnContext): Promise<void> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async onMessageActivity(turnContext: TurnContext): Promise<any> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async onTeamsSigninVerifyState(turnContext: TurnContext): Promise<any> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async onEventActivity(turnContext: TurnContext): Promise<any> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }
}
