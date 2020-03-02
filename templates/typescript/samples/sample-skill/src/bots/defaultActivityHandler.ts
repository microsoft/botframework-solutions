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
    DialogState, 
    DialogContext,
    DialogSet } from 'botbuilder-dialogs';
import { LocaleTemplateEngineManager, DialogEx } from 'botbuilder-solutions';

export class DefaultActivityHandler<T extends Dialog> extends ActivityHandler {
    private readonly dialog: Dialog;
    private readonly conversationState: BotState;
    private readonly userState: BotState;
    private dialogStateAccessor: StatePropertyAccessor<DialogState>;
    private templateEngine: LocaleTemplateEngineManager;
    private readonly dialogs: DialogSet;
    private readonly rootDialogId: string;

    public constructor(
        conversationState: BotState,
        userState: BotState,
        dialog: T,
        templateEngine: LocaleTemplateEngineManager
    ) {
        super();
        this.dialog = dialog;
        this.rootDialogId = dialog.id;
        
        this.conversationState = conversationState;
        this.userState = userState;
        this.dialogStateAccessor = conversationState.createProperty<DialogState>('DialogState');
        this.templateEngine = templateEngine

        this.dialogs = new DialogSet(this.dialogStateAccessor);
        this.dialogs.add(dialog);
        this.onTurn(this.turn.bind(this));
        this.onMembersAdded(this.membersAdded.bind(this));
    }

    public async turn(turnContext: TurnContext, next: () => Promise<void>): Promise<void> {
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

    protected async membersAdded(turnContext: TurnContext, next: () => Promise<void>): Promise<void> {
        await turnContext.sendActivity(this.templateEngine.generateActivityForLocale('IntroMessage'));
        await DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async onMessageActivity(turnContext: TurnContext): Promise<void> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected async onEventActivity(turnContext: TurnContext): Promise<void> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }
}
