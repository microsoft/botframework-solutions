/**
 * Copyright(c) Microsoft Corporation.All rights reserved.
 * Licensed under the MIT License.
 */

import {
    Activity,
    ActivityHandler,
    ActivityTypes,
    BotState,
    BotTelemetryClient,
    Channels,
    StatePropertyAccessor, 
    TurnContext } from 'botbuilder';
import {
    Dialog,
    DialogState,
    runDialog } from 'botbuilder-dialogs';
import { LocaleTemplateManager } from 'bot-solutions';

export class DefaultActivityHandler<T extends Dialog> extends ActivityHandler {
    private readonly dialog: Dialog;
    private readonly conversationState: BotState;
    private readonly userState: BotState;
    private readonly dialogStateAccessor: StatePropertyAccessor<DialogState>;
    private readonly templateEngine: LocaleTemplateManager;

    public constructor(conversationState: BotState, userState: BotState, templateManager: LocaleTemplateManager, telemetryClient: BotTelemetryClient, dialog: T) {
        super();
        this.dialog = dialog;
        this.dialog.telemetryClient = telemetryClient;
        this.conversationState = conversationState;
        this.userState = userState;
        this.dialogStateAccessor = conversationState.createProperty<DialogState>('DialogState');
        this.templateEngine = templateManager;
        super.onMembersAdded(this.membersAdded.bind(this));
    }

    public async onTurnActivity(turnContext: TurnContext): Promise<void> {
        await super.onTurnActivity(turnContext);

        // Save any state changes that might have occured during the turn.
        await this.conversationState.saveChanges(turnContext, false);
        await this.userState.saveChanges(turnContext, false);
    }

    protected async membersAdded(turnContext: TurnContext, next: () => Promise<void>): Promise<void> {
        await turnContext.sendActivity(this.templateEngine.generateActivityForLocale('IntroMessage', turnContext.activity.locale));
        await runDialog(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected onMessageActivity(turnContext: TurnContext): Promise<void> {
        // directline speech occasionally sends empty message activities that should be ignored
        const activity: Activity = turnContext.activity;
        if (activity.channelId === Channels.DirectlineSpeech &&
            activity.type === ActivityTypes.Message &&
            (activity.text === undefined || activity.text.trim().length === 0)) {
            return Promise.resolve();
        }
        return runDialog(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected onEventActivity(turnContext: TurnContext): Promise<void> {
        return runDialog(this.dialog, turnContext, this.dialogStateAccessor);
    }

    protected onEndOfConversationActivity(turnContext: TurnContext): Promise<void> {
        return runDialog(this.dialog, turnContext, this.dialogStateAccessor);
    }
}
