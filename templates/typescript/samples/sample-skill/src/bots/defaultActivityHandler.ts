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
import { LocaleTemplateEngineManager, DialogEx } from 'botbuilder-solutions';

export class DefaultActivityHandler<T extends Dialog> extends ActivityHandler {
    private readonly dialog: Dialog;
    private readonly conversationState: BotState;
    private readonly userState: BotState;
    private dialogStateAccessor: StatePropertyAccessor<DialogState>;
    private templateEngine: LocaleTemplateEngineManager;

    public constructor(conversationState: BotState, userState: BotState, templateEngine: LocaleTemplateEngineManager, dialog: T) {
        super();
        this.dialog = dialog;
        this.conversationState = conversationState;
        this.userState = userState;
        this.dialogStateAccessor = conversationState.createProperty<DialogState>('DialogState');
        this.templateEngine = templateEngine;
        super.onMembersAdded(this.membersAdded.bind(this));
    }

    public async onTurnActivity(turnContext: TurnContext): Promise<void> {
        await super.onTurnActivity(turnContext);

        // Save any state changes that might have occured during the turn.
        await this.conversationState.saveChanges(turnContext, false);
        await this.userState.saveChanges(turnContext, false);
    }

    protected async membersAdded(turnContext: TurnContext, next: () => Promise<void>): Promise<void> {
        await turnContext.sendActivity(this.templateEngine.generateActivityForLocale('IntroMessage'));
        await DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected onMessageActivity(turnContext: TurnContext): Promise<void> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected onEventActivity(turnContext: TurnContext): Promise<void> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected onEndOfConversationActivity(turnContext: TurnContext): Promise<void> {
        return DialogEx.run(this.dialog, turnContext, this.dialogStateAccessor);
    }
}
