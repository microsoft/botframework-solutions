/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    ConversationState,
    TurnContext, 
    UserState,
    TeamsActivityHandler,
    StatePropertyAccessor } from 'botbuilder';
import {
    Dialog,
    DialogContext,
    DialogSet,
    DialogState } from 'botbuilder-dialogs';
import { DialogEx } from 'botbuilder-solutions';

export class DefaultActivityHandler<T extends Dialog> extends TeamsActivityHandler {
    private readonly solutionName: string = '<%=assistantNameCamelCase%>';
    private readonly rootDialogId: string;
    private readonly dialogs: DialogSet;
    private readonly dialog: Dialog;
    private dialogStateAccessor: StatePropertyAccessor;

    public constructor(
        conversationState: ConversationState,
        userState: UserState,
        dialog: T) {
        super();

        this.dialog = dialog;
        this.rootDialogId = this.dialog.id;
        this.dialogs = new DialogSet(conversationState.createProperty<DialogState>(this.solutionName));
        this.dialogs.add(this.dialog);
        this.dialogStateAccessor = conversationState.createProperty<DialogState>('DialogState');
        this.onTurn(this.turn.bind(this));
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/tslint/config
    public async turn(turnContext: TurnContext, next: () => Promise<void>): Promise<any> {
        const dc: DialogContext = await this.dialogs.createContext(turnContext);

        if (dc.activeDialog !== undefined) {
            await dc.continueDialog();
        } else {
            await dc.beginDialog(this.rootDialogId);
        }

        await next();
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
