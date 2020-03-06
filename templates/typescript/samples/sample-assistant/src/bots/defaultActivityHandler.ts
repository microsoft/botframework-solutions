/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ConversationState,
    TurnContext, 
    UserState,
    TeamsActivityHandler,
    BotState,
    StatePropertyAccessor } from 'botbuilder';
import {
    Dialog,
    DialogContext,
    DialogSet,
    DialogState } from 'botbuilder-dialogs';
import { DialogEx } from 'botbuilder-solutions';

export class DefaultActivityHandler<T extends Dialog> extends TeamsActivityHandler {
    private readonly conversationState: BotState;
    private readonly userState: BotState;
    private readonly solutionName: string = 'sampleAssistant';
    private readonly rootDialogId: string;
    private readonly dialogs: DialogSet;
    private readonly dialog: Dialog;
    private dialogStateAccessor: StatePropertyAccessor;

    public constructor(
        conversationState: ConversationState,
        userState: UserState,
        dialog: T) {
        super();
        this.conversationState = conversationState;
        this.userState = userState;
        this.dialog = dialog;
        this.rootDialogId = this.dialog.id;
        this.dialogs = new DialogSet(conversationState.createProperty<DialogState>(this.solutionName));
        this.dialogs.add(this.dialog);
        this.dialogStateAccessor = conversationState.createProperty<DialogState>('DialogState');
        this.onTurn(this.turn.bind(this));
        this.onMembersAdded(this.membersAdded.bind(this));
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/tslint/config
    public async turn(turnContext: TurnContext, next: () => Promise<void>): Promise<any> {
        super.onTurn(next);

        const dc: DialogContext = await this.dialogs.createContext(turnContext);

        if (dc.activeDialog !== undefined) {
            await dc.continueDialog();
        } else {
            await dc.beginDialog(this.rootDialogId);
        }
        // Save any state changes that might have occured during the turn.
        await this.conversationState.saveChanges(turnContext, false);
        await this.userState.saveChanges(turnContext, false);
    }

    protected async membersAdded(turnContext: TurnContext, next: () => Promise<void>): Promise<any> {
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
